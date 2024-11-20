using ImageContrast;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;

namespace DXT3_to_text
{
    public partial class Form1 : Form
    {
        ImageDecode imageDecode;
        Bitmap image;
        int bitStride = 8;
        int minOccLen = 1;
        public Form1()
        {
            InitializeComponent();
            minOccLen = num(textBox2.Text);
        }

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
                Bitmap image_to_decode = new Bitmap(files[0]);
                imageDecode = new ImageDecode(image_to_decode, bitStride, (int)numericUpDown1.Value);
                image = image_to_decode;
                Bitmap transformedBtm = null;
                transformedBtm = image_to_decode.CopyToSquareCanvas(pictureBox1.Width);
                pictureBox1.Image = transformedBtm;


                richTextBox1.Text = Encoding.ASCII.GetString(imageDecode.decodedChars);
                runThreaded();
            }
        }
        private void ThreadsWait(ref Thread[] threads)
        {
            for (int i = 0; i < threads.Length; i++)
            {
                while (threads[i].IsAlive)
                {
                    if (imageDecode.logUpdate)
                    {
                        Invoke(new Action(() => richTextBox2.Text = imageDecode.getLog()));
                    }
                    Invoke(new Action(() => progressBar1.Value = (int)(((float)imageDecode.progress / imageDecode.numDictionaryWords) * 100.0)));
                    Thread.Sleep(100);
                }
            }
            Invoke(new Action(() => progressBar1.Value = 100));
        }
        private void button1_Click(object sender, EventArgs e)
        {
            run();
        }
        void run()
        {
            imageDecode.log("Processing " + image.Width + "x" + image.Height);
            int numThreads = (int)numericUpDown1.Value;
            //imageDecode.dictionaryCheck(minOccLen, 3, 4);
            Thread[] threads = new Thread[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                ThreadInfo ti = new ThreadInfo();
                ti.minOccLen = minOccLen;
                ti.threadID = i;
                ti.numThreads = numThreads;
                threads[i] = new Thread(new ParameterizedThreadStart(imageDecode.dictionaryCheck));
                threads[i].Start(ti);
            }
            ThreadsWait(ref threads);
            imageDecode.dictionaryStitch(numThreads);
            File.WriteAllBytes("Decode_Raw.txt", imageDecode.getStitchedDictionaryBytes());
            Invoke(new Action(() => progressBar1.Enabled = false));
        }
        void runThreaded()
        {
            Thread thread = new Thread(new ThreadStart(run));
            thread.Start();
            
        }
        int num(string a)
        {
            try
            {
                return Convert.ToInt32(a);
            }
            catch { return 0; }
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            update();
        }
        void update()
        {
            try
            {
                bitStride = num(textBox1.Text);
                minOccLen = num(textBox2.Text);
            }
            catch { }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            update();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void progressBar1_EnabledChanged(object sender, EventArgs e)
        {
            if (!progressBar1.Enabled)
            {
                Form2 f2 = new Form2(imageDecode.word, imageDecode.wordCount, imageDecode.language, image, bitStride);
                f2.ShowDialog();
            }
            progressBar1.Enabled = true;
        }
    }
}
