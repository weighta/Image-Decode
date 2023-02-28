using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DXT3_to_text
{
    public class ImageDecode
    {

        public string[] language = new string[] { "EN", "GE" };
        public string fileName = "TEXT.txt";
        public string analysedText;
        public int bitStride = 8;
        Bitmap btm;

        public ImageDecode(Bitmap btm, int bitStride)
        {
            this.btm = btm;
            this.bitStride = bitStride;
            Process();
        }

        void Process()
        {
            string text = "";
            int a = 0;
            int c = 0;
            for (int i = 0; i < (btm.Width * btm.Height) - 1; i++)
            {
                int row = i / btm.Width;
                int col = i % btm.Width;
                int row1 = (i + 1) / btm.Width;
                int col1 = (i + 1) % btm.Width;

                if ((col == 0) && (i != 0))
                {
                    text += "\n";
                }

                if (!colorEqual(btm.GetPixel(col, row), btm.GetPixel(col1, row1))) //place 1 if not equal, meaning data exists
                {
                    c |= (1 << ((bitStride - 1) - a));
                }
                a++;
                if (a == bitStride)
                {
                    text += Convert.ToChar((c % 26) + 97);
                    c = 0;
                    a = 0;
                }
            }
            analysedText = text;
            File.WriteAllText(fileName, analysedText);
        }
        bool colorEqual(Color a, Color b)
        {
            return (a.R == b.R) && (a.G == b.G) && (a.B == b.B);
        }
        public string dictionaryCheck(int minlength)
        {
            string[] lines1 = File.ReadAllLines(fileName);
            char[][] lines = new char[lines1.Length][];
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines1[i].ToCharArray();
            }
            
            for (int m = 0; m < language.Length; m++)
            {
                string findings = "";
                string[] dict1 = File.ReadAllLines(language[m] + ".txt");
                char[][] dict = new char[dict1.Length][];
                for (int i = 0; i < dict1.Length; i++)
                {
                    dict[i] = dict1[i].ToCharArray();
                }

                for (int i = 0; i < dict.Length; i++) //word to search
                {
                    if (dict[i].Length >= minlength)
                    {
                        for (int j = 0; j < lines.Length; j++)
                        {
                            if (dict[i].Length <= lines[j].Length)
                            {
                                for (int k = 0; k < lines[j].Length - (dict[i].Length - 1); k++)
                                {
                                    if (lines[j][k] == dict[i][0])
                                    {
                                        bool match = true;
                                        for (int l = 1; l < dict[i].Length; l++)
                                        {
                                            if (dict[i][l] != lines[j][k + l])
                                            {
                                                match = false;
                                                break;
                                            }
                                        }
                                        if (match)
                                        {
                                            findings += new string(dict[i]) + " (" + (k * bitStride) + ", " + (j) + ")\n";
                                        }
                                    }
                                }
                            }
                            else
                            {
                                j = lines.Length;
                            }
                        }
                    }
                    if (i == 100000)
                    {
                        Console.WriteLine(language[m] + " Quarter of the way, don't stop!");
                    }
                }
                
                try
                {
                    File.WriteAllText("decode\\" + bitStride + "\\" + language[m] + "_decode.txt", findings);
                }
                catch
                {
                    Directory.CreateDirectory("decode\\" + bitStride + "\\");
                    File.WriteAllText("decode\\" + bitStride + "\\" + language[m] + "_decode.txt", findings);
                }

                Console.WriteLine(language[m] + " done");
            }
            return "done";
        }
    }
}
