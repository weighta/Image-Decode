using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Reflection.Emit;
using System.Threading;
using ImageContrast;

namespace DXT3_to_text
{
    public struct ThreadInfo
    {
        public int minOccLen;
        public int threadID;
        public int numThreads;
    }
    public struct WordMatch
    {
        public short lang;
        public string Word;
        public int freq;
        public List<Point> loc;
    }
    public class ImageDecode
    {
        public bool logUpdate = false;
        public string logText = "";

        public int threadCount;
        public const int MAX_THREADS = 8;
        public string[][] dict;
        public byte[] decodedChars;
        public string[] language = new string[] { "EN", "GE" };
        public short[] langShorts;
        public string fileName = "TEXT.txt";
        public int bitStride = 8;
        public WordMatch[][] word = new WordMatch[MAX_THREADS][];
        public int[] wordCount;

        public byte[][] findings;
        public int[] findingsCount;
        public byte[] findings_stitched;
        public int stitchedCount;

        public int progress = 0;
        public int numDictionaryWords=0;

        Bitmap btm;
        int width;
        int height;

        public ImageDecode(Bitmap btm, int bitStride, int threadCount)
        {
            this.threadCount = threadCount;
            this.btm = btm;
            this.bitStride = bitStride;
            width = btm.Width;
            height = btm.Height;

            findings = new byte[MAX_THREADS][]; //Maximum of 8 threads
            findingsCount = new int[MAX_THREADS];
            for (int i = 0; i < findings.Length; i++)
            {
                findings[i] = new byte[(1 << 20)];
            }
            findings_stitched = new byte[(1 << 23)];

            for (int i = 0; i < word.Length; i++)
            {
                word[i] = new WordMatch[1 << 15];
                for (int j = 0; j < word[i].Length; j++)
                {
                    word[i][j].loc = new List<Point>();
                }
            }

            dict = new string[language.Length][];
            wordCount = new int[MAX_THREADS];
            for (int i = 0; i < dict.Length; i++) //language length
            {
                dict[i] = File.ReadAllLines(language[i] + ".txt");
                numDictionaryWords += dict[i].Length;
            }
            langShorts = new short[language.Length];
            for (int i = 0; i < langShorts.Length; i++)
            {
                langShorts[i] = Tools.stringToShort(language[i]);
            }
            Process();
        }

        void Process()
        {
            decodedChars = btm.char_stride_Decode(bitStride);

            File.WriteAllBytes(fileName, decodedChars);
        }
        bool colorEqual(Color a, Color b)
        {
            return (a.R == b.R) && (a.G == b.G) && (a.B == b.B);
        }
        public void dictionaryCheck(object obj)
        {
            ThreadInfo p = (ThreadInfo)obj;
            dictionaryCheck(p.minOccLen, p.threadID, p.numThreads);
        }
        public string dictionaryCheck(int minlength, int threadID, int numThreads)
        {
            string[] lines = File.ReadAllLines(fileName);
            for (int m = 0; m < language.Length; m++)
            {
                short lang = Tools.stringToShort(language[m]);
                int wordsBetween = dict[m].Length / numThreads;
                int start = wordsBetween * threadID;
                int end = wordsBetween * (threadID + 1);
                if (threadID == numThreads - 1) end = dict[m].Length;

                for (int i = start; i < end; i++) //word to search
                {
                    string WORD = dict[m][i];
                    if (WORD.Length >= minlength)
                    {
                        for (int j = 0; j < lines.Length; j++)
                        {
                            if (WORD.Length <= lines[j].Length)
                            {
                                for (int k = 0; k < lines[j].Length - (WORD.Length); k++)
                                {
                                    if (lines[j][k] == WORD[0])
                                    {
                                        bool match = true;
                                        for (int l = 1; l < WORD.Length; l++)
                                        {
                                            if (WORD[l] != lines[j][k + l])
                                            {
                                                match = false;
                                                break;
                                            }
                                        }
                                        if (match)
                                        {
                                            //int currSum = sum(WORD);
                                            if (!exists(WORD, threadID, m))
                                            {
                                                word[threadID][wordCount[threadID]].lang = lang;
                                                word[threadID][wordCount[threadID]].Word = WORD;
                                                word[threadID][wordCount[threadID]].loc.Add(coordsFromWidth((k * bitStride)));
                                                word[threadID][wordCount[threadID]].freq = 1;
                                                byte[] bytes = Encoding.ASCII.GetBytes(word[threadID][wordCount[threadID]].Word + " (" + word[threadID][wordCount[threadID]].loc[0].X + ", " + word[threadID][wordCount[threadID]].loc[0].Y + ")");
                                                bytes.CopyTo(findings[threadID], findingsCount[threadID]);
                                                findings[threadID][findingsCount[threadID] + bytes.Length] = 0x0A;
                                                findingsCount[threadID] += bytes.Length + 1;
                                                wordCount[threadID]++;
                                                k += WORD.Length - 1;
                                               //prevSum = currSum;
                                            }
                                            else
                                            {
                                                int ind = wordCount[threadID] - 1;
                                                Point coordsToAdd = coordsFromWidth((k * bitStride));
                                                bool exists = false;
                                                for (int l = 0; l < word[threadID][ind].loc.Count(); l++) //Go through all the points
                                                { //to make sure it doesnt exist
                                                    if (word[threadID][ind].loc[l].Equals(coordsToAdd))
                                                    {
                                                        exists = true;
                                                        break;
                                                    }
                                                }
                                                if (!exists)
                                                {
                                                    word[threadID][ind].loc.Add(coordsToAdd);
                                                    word[threadID][ind].freq++;
                                                }
                                            }
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
                    progress++;
                }
                log(language[m] + " ThreadID[" + threadID + "] done");
            }
            return "Finished.";
        }
        private Point coordsFromWidth(int x)
        {
            return new Point(x % width, x / width);
        }
        public void log(string a)
        {
            logText += a + "\n";
            logUpdate = true;
        }
        public string getLog()
        {
            logUpdate = false;
            return logText;
        }

        public bool exists(string WORD, int threadID, int langIndex)
        {
            for (int i = 0; i < wordCount[threadID]; i++)
            {
                if (word[threadID][i].Word == WORD && word[threadID][i].lang == langShorts[langIndex])
                {
                    return true;
                }
            }
            return false;
        }
        public int sum(string b)
        {
            int a = 0;
            for (int i = 0; i < b.Length; i++)
            {
                a += b[i];
            }
            return a;
        }
        public void dictionaryStitch(int numThreads)
        {
            int stitchOffs = 0;
            for (int i = 0; i < numThreads; i++)
            {
                findings[i].CopyTo(findings_stitched, stitchOffs);
                stitchOffs += findingsCount[i];
            }
            stitchedCount = stitchOffs;
        }
        public byte[] getStitchedDictionaryBytes()
        {
            byte[] ret = new byte[stitchedCount];
            for (int i = 0; i < stitchedCount; i++)
            {
                ret[i] = findings_stitched[i];
            }
            return ret;
        }
    }
}
