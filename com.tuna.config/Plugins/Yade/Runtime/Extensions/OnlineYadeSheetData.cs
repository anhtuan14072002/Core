//  Copyright (c) 2022-present amlovey
//  
using System.Text.RegularExpressions;
using System.Collections;
using Yade.Runtime.CSV;
using UnityEngine.Networking;

namespace Yade.Runtime
{
    /// <summary>
    /// Yade Sheet of an online url
    /// </summary>
    public abstract class OnlineSheet
    {
        protected string url;
        protected string content;

        public OnlineSheet(string url)
        {
            this.url = url;
        }

        abstract protected string PrepareUrl();
        abstract public IEnumerator DownloadSheetData();
        abstract public YadeSheetData GetYadeSheetData();
    }

    /// <summary>
    /// Download data form online CSV content and parse to Yade Sheet
    /// </summary>
    public class OnlineCSVSheet : OnlineSheet
    {
        public OnlineCSVSheet(string url) : base(url) 
        {

        }

        protected override string PrepareUrl()
        {
            return url;
        }

        /// <summary>
        /// Donload data of online sheet 
        /// </summary>
        public override IEnumerator DownloadSheetData()
        {
            UnityWebRequest webRequest = new UnityWebRequest(this.PrepareUrl());
            webRequest.method = "GET";
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            yield return webRequest.SendWebRequest();
            if (!string.IsNullOrEmpty(webRequest.error))
            {
                yield break;
            }

            var downloadText = webRequest.downloadHandler.text;
            if (downloadText.Contains("google-site-verification"))
            {
                this.content = string.Empty;   
            }
            else
            {
                this.content = webRequest.downloadHandler.text;
            }
        }

        /// <summary>
        /// Conver downloaded sheet data to Yade Sheet instance
        /// </summary>
        public override YadeSheetData GetYadeSheetData()
        {
            YadeSheetData sheet = YadeSheetData.CreateInstance<YadeSheetData>();
            if (string.IsNullOrEmpty(this.content))
            {
                return null;
            }

            YadeCSVReader reader = new YadeCSVReader(this.content);
            YadeCSVCell cell;
            while ((cell = reader.Read()) != null)
            {
                if (!string.IsNullOrEmpty(cell.Value))
                {
                    sheet.SetRawValue(cell.Row, cell.Column, cell.Value);
                }
            }

            sheet.RecalculateValues();
            return sheet;
        }
    }

    /// <summary>
    /// Download data from Google Sheet (public share link) and parse to Yade Sheet
    /// </summary>
    public class OnlineGoogleSheet : OnlineCSVSheet
    {
        private string gid;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="url">Public google share link</param>
        /// <param name="gid">GID of the sheet</param>
        public OnlineGoogleSheet(string url, string gid = "") : base (url)
        {
            this.gid = gid;
        }

        protected override string PrepareUrl()
        {
            var pattern = new Regex(@"/(?<Source>[a-zA-z]+)?/d/(?<Id>\S+)?/");
            var match = pattern.Match(url);
            if (match != null)
            {
                var source = match.Groups["Source"].Value;
                var id = match.Groups["Id"].Value;
                var gidParameter = string.Empty;

                if (!string.IsNullOrEmpty(this.gid))
                {
                    gidParameter = string.Format("&gid={0}", this.gid);
                }

                return string.Format("https://docs.google.com/{0}/d/{1}/export?format=csv&id={1}{2}&_{3}", source, id, gidParameter, UnityEngine.Random.Range(0, int.MaxValue));
            }

            return string.Empty;
        }
    }
}