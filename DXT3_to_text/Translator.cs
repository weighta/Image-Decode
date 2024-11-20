using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net;


namespace DXT3_to_text
{
    public class Translator
    {
        Translator()
        {

        }
        /// <summary>
        /// SOURCE FROM [IT Core Soft] yt - https://www.youtube.com/watch?v=DDp4LQ_0o6Q
        /// https://www.youtube.com/@itcoresoft5761
        /// </summary>
        /// <param name="input"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static String translate(String input, string from, string to)
        {
            //Not my code
            var fromLanguage = from;
            var toLanguage = to;
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={HttpUtility.UrlEncode(input)}";
            var webclient = new WebClient
            {
                Encoding = System.Text.Encoding.UTF8
            };
            var result = webclient.DownloadString(url);
            try
            {
                result = result.Substring(4, result.IndexOf("\"", 4
                    , StringComparison.Ordinal) - 4);
                return result;
            }
            catch (Exception e1)
            {
                return "error\n" + e1;
            }
        }
    }

    
}
