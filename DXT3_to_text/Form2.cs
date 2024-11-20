using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
//using System.Web.UI.WebControls;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using CheckBox = System.Windows.Forms.CheckBox;
using Label = System.Windows.Forms.Label;
using ListView = System.Windows.Forms.ListView;
using Panel = System.Windows.Forms.Panel;
using View = System.Windows.Forms.View;
using ListViewItem = System.Windows.Forms.ListViewItem;
using BorderStyle = System.Windows.Forms.BorderStyle;

namespace DXT3_to_text
{
    public partial class Form2 : Form
    {
        CheckBox[] checkBoxWords;
        Label[][] labels;
        PictureBox pictureBox1;
        Graphics g;
        Pen[] pen;
        int penThickness;

        int stride;
        int spacing_X = 32;
        const int ALPHABET_COUNT = 26;
        const int MAX_LANGUAGE_COUNT = 10;
        WordMatch[][] Words;
        int[] wordCount;
        string[] languages;
        int[] languagesOcc;
        int[][] alphabetLaidOut;
        int[][] x_spacingMaxWordSize;
        int totalWords;
        int numThreads;
        Bitmap image;
        Bitmap bufferImage;
        Brush[] brush;
        List<WordMatch>[] checkedWords;
        int fontSize = 12;
        Font[] font;
        SaveFileDialog sfd;

        ListView[] listView;
        ColumnHeader[][] columnHeader;
        ListViewItem[][] listViewItem;
        RichTextBox[] coordsTextBox;

        public Form2(WordMatch[][] words, int[] wordCount, string[] languages, Bitmap image, int stride)
        {
            InitializeComponent();
            this.image = image;
            bufferImage = new Bitmap(image);
            g = Graphics.FromImage(bufferImage);
            brush = new Brush[2] { Brushes.Red, Brushes.LightBlue };
            pen = new Pen[brush.Length];
            for (int i = 0; i < pen.Length; i++) pen[i] = new Pen(brush[i]);
            checkedWords = new List<WordMatch>[languages.Length];
            for (int i = 0; i < checkedWords.Length; i++) checkedWords[i] = new List<WordMatch>();
            penThickness = 1 + (image.Width >> 7);
            font = new Font[trackBar1.Maximum];
            for (int i = 0; i < font.Length; i++) font[i] = new Font("Arial", 5 + ((i + 1) << 1), FontStyle.Regular, GraphicsUnit.Pixel); ;
            Words = words;
            this.wordCount = wordCount;
            this.languages = languages;
            this.stride = stride;
            languagesOcc = new int[MAX_LANGUAGE_COUNT];
            alphabetLaidOut = new int[MAX_LANGUAGE_COUNT][];
            for (int i = 0; i < alphabetLaidOut.Length; i++)
            {
                alphabetLaidOut[i] = new int[ALPHABET_COUNT];
            }
            x_spacingMaxWordSize = new int[MAX_LANGUAGE_COUNT][];
            for (int i = 0; i < x_spacingMaxWordSize.Length; i++)
            {
                x_spacingMaxWordSize[i] = new int[ALPHABET_COUNT];
            }
            listViewItem = new ListViewItem[languages.Length][];
            for (int i = 0; i < listViewItem.Length; i++)
            {
                listViewItem[i] = new ListViewItem[1 << 5];
                for (int j = 0; j < listViewItem[i].Length; j++) listViewItem[i][j] = new ListViewItem();
            }
            sfd = new SaveFileDialog() { Title = "Save Image", DefaultExt = "png", Filter = "png files (*.png)|*.png|All files (*.*)|*.*" };
            this.Text = "Discoveries for stride " + stride;
            getLangCounts();
        }


        private void Form2_Load(object sender, EventArgs e)
        {
            listView = new ListView[languages.Length];
            columnHeader = new ColumnHeader[languages.Length][];

            int[] columnSpacing = new int[2] { 137, 63 };
            string[] headers = new string[2] { "Word", "# Occur" };

            for (int i = 0; i < listView.Length; i++) //lang length
            {
                listView[i] = new ListView();
                columnHeader[i] = new ColumnHeader[2]; //three headers, Word, Freq, Location, etc.
                for (int j = 0; j < columnHeader.Length; j++)
                {
                    columnHeader[i][j] = new ColumnHeader();
                    columnHeader[i][j].Text = headers[j];
                    columnHeader[i][j].Width = columnSpacing[j];
                    columnHeader[i][j].TextAlign = HorizontalAlignment.Center;
                }
                listView[i].Activation = ItemActivation.OneClick;
                listView[i].BackColor = Color.White;
                listView[i].Columns.AddRange(columnHeader[i]);
                listView[i].ForeColor = Color.Black;
                listView[i].GridLines = true;
                listView[i].HideSelection = false;
                listView[i].HoverSelection = true;
                listView[i].LabelEdit = true;
                listView[i].LabelWrap = false;
                listView[i].Location = new Point(4, 4);
                listView[i].Size = new Size(380, 128);
                listView[i].TabIndex = 3;
                listView[i].UseCompatibleStateImageBehavior = false;
                listView[i].View = View.Details;
                listView[i].SelectedIndexChanged += new EventHandler(listView1_SelectedIndexChanged);
            }
            coordsTextBox = new RichTextBox[languages.Length];
            for (int i = 0; i < coordsTextBox.Length; i++) coordsTextBox[i] = new RichTextBox() { Location = new Point(listView[i].Location.X + listView[i].Width, listView[i].Location.Y), Width = 128, Height = listView[i].Height, BorderStyle = BorderStyle.FixedSingle };
            
            totalWords = getTotalWords();
            pictureBox1 = new PictureBox() { Location = new Point(tabControl1.Location.X + tabControl1.Width, tabControl1.Location.Y), Width = tabControl1.Height - 32, Height = tabControl1.Height - 32, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom};
            presentImage(image);
            
            Panel[] pa = new Panel[languages.Length];
            for (int i = 0; i < pa.Length; i++)
            {
                pa[i] = new Panel() { Location = new Point(0,0), BackColor = Color.White, Width = tabControl1.Width - 16, Height = 250,  AutoScroll = true }; //Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom,
            }
            Panel[] pa1 = new Panel[languages.Length];
            for (int i = 0; i < pa1.Length; i++)
            {
                pa1[i] = new Panel() { Location = new Point(pa[i].Location.X, pa[i].Location.Y + pa[i].Height), Width = pa[i].Width, Height = 140, BackColor = Color.White}; //Anchor = AnchorStyles.Bottom | AnchorStyles.Left 
                pa1[i].Controls.Add(listView[i]);
                pa1[i].Controls.Add(coordsTextBox[i]);
            }
            labels = new Label[languages.Length][];
            for (int i = 0; i < labels.Length; i++)
            {
                int initSpacing = 16;
                labels[i] = new Label[2] {
                    new Label() { Location = new Point(initSpacing, initSpacing), Text = "# Words: " + numWords(i), Width = 72, Height = 16 },
                    new Label() { Location = new Point(initSpacing + 85, initSpacing), Text = "", Width = pa1[0].Width, Height = 16 },
                    //new Label() { Location = new Point(initSpacing + 120 , initSpacing), Text = "", Width = pa1[0].Width, Height = 16 }
                };
            }
            for (int i = 0; i < languages.Length; i++)
            {
                for (int j = 0; j < labels.Length; j++)
                {
                    pa[i].Controls.Add(labels[i][j]);
                }
            }

            checkBoxWords = new CheckBox[totalWords];
            int a = 0;
            for (int i = 0; i < Words.Length; i++)
            {
                for (int j = 0; j < wordCount[i]; j++)
                {
                    WordMatch wm = Words[i][j];
                    int alphInitial = (wm.Word[0] - 97);
                    int langIndex = indexOfLang(wm.lang);

                    int width = 30 + (wm.Word.Length << 3);
                    if (x_spacingMaxWordSize[langIndex][alphInitial] < width)
                    {
                        x_spacingMaxWordSize[langIndex][alphInitial] = width;
                    }

                    int X_Shift = 0;
                    if (alphInitial == 0)
                    {
                        X_Shift = spacing_X;
                    }
                    else
                    { //b - z
                        X_Shift = spacing_X;
                        int widthAddition = 0;
                        for (int k = 0; k < alphInitial; k++)
                        {
                            widthAddition += x_spacingMaxWordSize[langIndex][k];
                        }
                        X_Shift += widthAddition;
                    }

                    int Y_Shift = (alphabetLaidOut[langIndex][alphInitial] * 22) + spacing_X;
                    checkBoxWords[a] = new CheckBox { BackColor = Color.Transparent, Name = langFromShort(wm.lang), Width = width, Text = wm.Word, Location = new Point(X_Shift, Y_Shift) };
                    checkBoxWords[a].CheckedChanged += checkBoxChecked;
                    pa[langIndex].Controls.Add(checkBoxWords[a]);
                    alphabetLaidOut[langIndex][alphInitial]++;
                    a++;
                }
            }

            TabPage[] tb = new TabPage[languages.Length];
            for (int i = 0; i < tb.Length; i++)
            {
                tb[i] = new TabPage() { Text = "language: " + languages[i] };
                tb[i].Controls.Add(pa[i]);
                tb[i].Controls.Add(pa1[i]);
            }
            Controls.Add(pictureBox1);

            for (int i = 0; i < tb.Length; i++)
            {
                tabControl1.TabPages.Add(tb[i]);
            }
        }

        void checkBoxChecked(object sender, EventArgs e)
        {
            markImage();
        }
        void markImage()
        {
            for (int i = 0; i < checkedWords.Length; i++) checkedWords[i].Clear();
            g.DrawImage(image, 0, 0);
            Font fontCurr = font[trackBar1.Value - 1];
            for (int i = 0; i < pen.Length; i++) pen[i].Width = 1;
            for (int i = 0; i < checkBoxWords.Length; i++)
            {
                if (checkBoxWords[i].Checked)
                {
                    int langIndex = indexOfLang(checkBoxWords[i].Name);
                    WordMatch word = wordSearch(checkBoxWords[i].Text, Tools.stringToShort(checkBoxWords[i].Name));
                    checkedWords[langIndex].Add(word);
                    int ellipse_width = stride * word.Word.Length;
                    int ellipse_height = 10 + (word.Word.Length << 1);
                    Pen p = pen[langIndex];
                    for (int j = 0; j < word.freq; j++)
                    {
                        int ellipse_x = word.loc[j].X;
                        int ellipse_y = word.loc[j].Y - (ellipse_height >> 1);

                        int x1 = word.loc[j].X + ellipse_width;
                        int y1 = word.loc[j].Y;

                        g.DrawEllipse(p, ellipse_x, ellipse_y, ellipse_width, ellipse_height); //Ellipse
                        if (checkBox1.Checked)
                        {
                            g.DrawLine(p, ellipse_x, y1, x1, y1); //Incentive Line
                        }
                        if (checkBox2.Checked)
                        {
                            int y2 = ellipse_y + ellipse_height > image.Height - fontSize ? ellipse_y - fontSize : ellipse_y + ellipse_height;
                            string text = word.Word;
                            if (checkBox4.Checked) text += " (" + word.loc[j].X + ", " + word.loc[j].Y + ")";
                            g.DrawString(text, fontCurr, brush[langIndex], ellipse_x, y2); //Name
                        }
                    }
                }
            }
            if (checkBox3.Checked)
            {
                for (int i = 0; i < checkedWords.Length; i++)
                {
                    for (int j = 0; j < checkedWords[i].Count() - 1; j++)
                    {
                        Pen p = pen[indexOfLang(checkedWords[i][j].lang)];
                        int x = checkedWords[i][j].loc[0].X;
                        int y = checkedWords[i][j].loc[0].Y;
                        int x1 = checkedWords[i][j + 1].loc[0].X;
                        int y1 = checkedWords[i][j + 1].loc[0].Y;
                        g.DrawLine(p, x, y, x1, y1);
                    }
                }
            }
            for (int i = 0; i < languages.Length; i++)
            {
                string txt = "";
                if (checkedWords[i].Count() > 0) txt = "Selected: ";
                for (int j = 0; j < checkedWords[i].Count(); j++)
                {
                    txt += checkedWords[i][j].Word + " (" + checkedWords[i][j].loc.Count() + ")";
                    if (j < checkedWords[i].Count() - 1) txt += ", ";
                }
                labels[i][1].Text = txt;
            }
            for (int i = 0; i < languages.Length; i++)
            {
                listView[i].Items.Clear();
                for (int j = 0; j < checkedWords[i].Count; j++)
                {
                    listViewItem[i][j].SubItems.Clear();
                    listViewItem[i][j].Text = checkedWords[i][j].Word;
                    listViewItem[i][j].SubItems.Add(checkedWords[i][j].freq + "");
                    listView[i].Items.Add(listViewItem[i][j]);
                }
            }

            presentImage(bufferImage);
        }

        void presentImage(Bitmap btm)
        {
            pictureBox1.Image = Tools.ResizeImage(btm, pictureBox1.Width, pictureBox1.Height);
        }
        int getTotalWords()
        {
            int totalWords = 0;
            for (int i = 0; i < wordCount.Length; i++)
            {
                totalWords += wordCount[i];
            }
            return totalWords;
        }
        short shortFromLang(int langIndex)
        {
            return (short)((languages[langIndex][0] << 8) | languages[langIndex][1]);
        }
        string langFromShort(short a)
        {
            return Convert.ToChar((byte)(a >> 8)) + "" + Convert.ToChar((byte)(a & 255));
        }
        WordMatch wordSearch(string text, short lang)
        {
            for (int i = 0; i < numThreads; i++)
            {
                for (int j = 0; j < wordCount[i]; j++)
                {
                    bool wordMatch = Words[i][j].Word.Equals(text);
                    bool langMatch = Words[i][j].lang == lang;
                    if (wordMatch)
                    {
                        if (langMatch)
                        {
                            return Words[i][j];
                        }
                    }
                }
            }
            throw new Exception("Word not found: " + text + " " + lang);
        }
        int numWords(int langIndex)
        {
            int numWords = 0;
            short lang = shortFromLang(langIndex);
            for (int i = 0; i < numThreads; i++)
            {
                for (int j = 0; j < wordCount[i]; j++)
                {
                    if (Words[i][j].lang == lang)
                    {
                        numWords++;
                    }
                }
            }
            return numWords;
        }
        void getLangCounts()
        {
            for (int i = 0; i < languages.Length; i++)
            {
                short occ = (short)(languages[i][0] << 8);
                occ |= (short)(languages[i][1]);
                for (int j = 0; j < wordCount.Length; j++) //Should be 8 because 8 max threads
                {
                    numThreads = j + 1;
                    if (wordCount[j] != 0)
                    {
                        for (int k = 0; k < wordCount[j]; k++)
                        {
                            if (Words[j][k].lang == occ)
                            {
                                languagesOcc[j]++;
                            }
                        }
                    }
                    else break;
                }
            }
            for (int i = 0; i < languages.Length; i++)
            {
                int a = 0;
                for (int j = ALPHABET_COUNT - 1 - i; j >= 0; j--)
                {
                    a += x_spacingMaxWordSize[i][j];
                }
                x_spacingMaxWordSize[i][ALPHABET_COUNT - 1 - i] = a;
            }
        }
        int indexOfLang(short lang)
        {
            for (int i = 0; i < languages.Length; i++)
            {
                short occ = (short)(languages[i][0] << 8);
                occ |= (short)(languages[i][1]);
                if (lang == occ)
                {
                    return i;
                }
            }
            return -1;
        }
        int indexOfLang(string lang)
        {
            for (int i = 0; i < languages.Length; i++)
            {
                if (lang.Equals(languages[i])) return i;
            }
            throw new Exception("language not found: " + lang);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            markImage();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = image;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            penThickness = trackBar1.Value;
            markImage();
        }

        private void Form2_Resize(object sender, EventArgs e)
        {
            presentImage(bufferImage);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                bufferImage.Save(sfd.FileName);
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView lv = ((ListView)sender);
            if (lv.SelectedIndices.Count <= 0)
                return;
            if (lv.SelectedIndices[0] < 0)
                return;
            int langIndex = tabControl1.SelectedIndex;
            string coords = "X\tY";
            for (int i = 0; i < lv.SelectedIndices.Count; i++)
            {
                string text = lv.Items[lv.SelectedIndices[i]].Text;
                for (int j = 0; j < checkedWords[langIndex].Count(); j++)
                {
                    if (checkedWords[langIndex][j].Word == text)
                    {
                        for (int k = 0; k < checkedWords[langIndex][j].loc.Count; k++)
                        {
                            coords += "\n"+checkedWords[langIndex][j].loc[k].X + "\t" + checkedWords[langIndex][j].loc[k].Y;
                        }
                    }
                }
                coordsTextBox[langIndex].Text = coords;
            }
            
        }
    }
}
