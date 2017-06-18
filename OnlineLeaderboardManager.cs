using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Phone.Info;

namespace APGFramework.Leaderboard
{
    public class highscoreTable
    {
        public string[] ranks = new string[20];
        public string[] names = new string[20];
        public string[] infos = new string[20];
        public int[] scores = new int[20];

        public highscoreTable()
        {
        }

        public highscoreTable(string[] ranks, string[] names, string[] infos, int[] scores)
        {
            this.ranks = ranks;
            this.names = names;
            this.infos = infos;
            this.scores = scores;
        }
    }

    public class OnlineLeaderboardManager
    {
        const string REQUEST_METHOD_POST = "POST";
        const string CONTENT_TYPE = "application/x-www-form-urlencoded";

        string responseString;
        public highscoreTable result;
        public bool isRequestFinished;
        public bool isResultOK;

        private string webPost(string _URI, string _postString)
        {
            isResultOK = false;
            isRequestFinished = false;
            Stream dataStream = null;
            StreamReader reader = null;
            HttpWebResponse response = null;
            responseString = null;
            Uri uri = new Uri(_URI);

            // Create a request using a URL that can receive a post.
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
            // Set the Method property of the request to POST.
            request.Method = REQUEST_METHOD_POST;
            // Create POST data and convert it to a byte array.
            string postData = _postString;
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            // Set the ContentType property of the WebRequest.
            request.ContentType = CONTENT_TYPE;
            // Set the ContentLength property of the WebRequest.
            //request.ContentLength = byteArray.Length;
            // Get the request stream.
            dataStream = HttpWebRequestExtensions.GetRequestStream((HttpWebRequest)request);// request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();
            // Get the response.
            var callback = new AsyncCallback(delegate(IAsyncResult asynchronousResult)
            {
                try
                {
                    response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
                    dataStream = response.GetResponseStream();
                    // Open the stream using a StreamReader for easy access.
                    reader = new StreamReader(dataStream);
                    // Read the content.
                    responseString = reader.ReadToEnd();
                    // Display the content.
                    //Console.WriteLine(responseFromServer);
                    // Clean up the streams.
                    if (reader != null) reader.Close();
                    if (dataStream != null) dataStream.Close();
                    if (response != null) response.Close();
                    result = parseToHighscoreTable(responseString);

                    isResultOK = true;
                }
                catch (Exception ex)
                {
                    isResultOK = false;
                }

                isRequestFinished = true;
            });

            response = HttpWebRequestExtensions.GetResponse((HttpWebRequest)request, callback);
            return responseString;
        }

        private string webPost1(string _URI, string _postString)
        {
            var client = new WebClient();
            client.DownloadStringCompleted += (s, ev) =>
            {
                result = parseToHighscoreTable(ev.Result);
            };
            string str = "http://ilinyevg.nichost.ru/Leaderboard/requestscores.php";
            UriBuilder uri = new UriBuilder(str);
            uri.Query = "ModeID=1&Format=TOP10";

            client.DownloadStringAsync(uri.Uri);//.DownloadStringAsync

            return responseString;
        }

        public void sendOnlineRequest(string modeid, string format)
        {
            string postString = "ModeID=" + modeid + "&Format=" + format;
            webPost("http://ilinyevg.nichost.ru/Leaderboard/requestscores.php", postString);
        }

        private highscoreTable parseToHighscoreTable(string tableString)
        {
            const string SERVER_VALID_DATA_HEADER = "SERVER_";
            if (tableString.Trim().Length < SERVER_VALID_DATA_HEADER.Length ||
            !tableString.Trim().Substring(0, SERVER_VALID_DATA_HEADER.Length).Equals(SERVER_VALID_DATA_HEADER)) return null;
            string toParse = tableString.Trim().Substring(SERVER_VALID_DATA_HEADER.Length);
            string[] ranks = new string[20];
            string[] names = new string[20];
            string[] infos = new string[20];
            int[] scores = new int[20];
            string[] rows = Regex.Split(toParse, "_ROW_");
            for (int i = 0; i < 20; i++)
            {
                if (rows.Length > i && rows[i].Trim() != "")
                {
                    string[] cols = Regex.Split(rows[i], "_COL_");
                    if (cols.Length == 4)
                    {
                        names[i] = cols[0].Trim();
                        infos[i] = cols[1].Trim();
                        scores[i] = int.Parse(cols[2]);
                        ranks[i] = cols[3];
                    }
                }
                else
                {
                    names[i] = "";
                    infos[i] = "";
                    scores[i] = 0;
                    ranks[i] = "";
                }
            }
            return new highscoreTable(ranks, names, infos, scores);
        }

        private string hashString(string _value)
        {
            // MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.UTF8.GetBytes(_value);//ASCII
            //data = x.ComputeHash(data);
            string ret = "";
            ret = MD5Core.GetHashString(data).ToLower();
            //ret = "caf516972d9eb45114b9d13d9fa1b884";
            //for (int i = 0; i < data.Length; i++) ret += data[i].ToString("x2").ToLower();
            return ret;
        }

        public string sendScore(string modeid, string name, string info, int score, int currentLevelId, int totalPlaySecs)
        {
            //name = "evgen";
            //score = 7700;
            string highscoreString = name + info + score + "galina24182421";
            string postString = "ModeID=" + modeid
                            + "&Name=" + name
                            + "&Info=" + info
                            + "&Score=" + score
                            + "&DeviceUniqueId=" + GetDeviceUniqueId()
                            + "&CurrentLevel=" + currentLevelId.ToString()
                            + "&TotalPlayTime=" + totalPlaySecs.ToString()
                            + "&Hash=" + hashString(highscoreString);
            string response = null;
            response = webPost("http://ilinyevg.nichost.ru/Leaderboard/newscoreGR.php", postString);
            return response;
        }

        public string sendRunInfo(string modeid, string info, int currentLevelId, int totalPlaySecs)
        {
            //name = "evgen";
            //score = 7700;

            string highscoreString = "Robot" + info + "0" + "galina24182421";
            string postString = "ModeID=" + modeid
                            + "&Name=Robot"
                            + "&Info=" + info
                            + "&Score=0"
                            + "&DeviceUniqueId=" + GetDeviceUniqueId()
                            + "&CurrentLevel=" + currentLevelId.ToString()
                            + "&TotalPlayTime=" + totalPlaySecs.ToString()
                            + "&Hash=" + hashString(highscoreString);
            string response = null;
            response = webPost("http://ilinyevg.nichost.ru/Leaderboard/newscoreGR.php", postString);
            return response;
        }

        private string GetDeviceUniqueId()
        {
            object DeviceUniqueID;

            byte[] DeviceIDbyte = null;

            if (DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out DeviceUniqueID))
                DeviceIDbyte = (byte[])DeviceUniqueID;

            return Convert.ToBase64String(DeviceIDbyte);

        }
    }
}
