using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DXT3_to_text
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        ImageDecode ID;
        int bitStride = 8;
        int minOccLen = 1;

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            for (int i = 0; i < files.Length; i++)
            {
                ID = new ImageDecode(new Bitmap(files[0]), bitStride);
                richTextBox1.Text = ID.analysedText;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = ID.dictionaryCheck(minOccLen);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            update();
        }
        void update()
        {
            try
            {
                bitStride = Convert.ToInt32(textBox1.Text);
                minOccLen = Convert.ToInt32(textBox2.Text);
            }
            catch
            {

            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            update();
        }
    }
}
