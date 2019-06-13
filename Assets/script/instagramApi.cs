using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Linq;

//Unity C#でJSONの扱い方
//http://qiita.com/phi/items/914bc839b543988fc0ec
//http://qiita.com/asus4/items/bac121c34cd3169116c0
using MiniJSON;
//Also Download JsonNode


public class InstagramApi : MonoBehaviour
{
        public string access_token;
        public string username;
        public GameObject[] PhotoFrames;

        public InstagramApi(string access_token)
        {
            this.access_token = access_token;
        }

    void Start()
    {
        StartCoroutine("MainRoutine");
    }


    /// <summary>
    /// 設定された情報を元にインスタグラムにアクセスしてフォトフレームを書き換えます
    /// </summary>
    private IEnumerator<bool> MainRoutine()
    {
        //未設定チェック
        if (username == "") { Debug.Log("username is not set");return null; }
        if (access_token == "") { Debug.Log("access token is not set"); return null; }
        if (PhotoFrames.Length==0) { Debug.Log("PhotoFrames is not set"); return null; }

        //メイン処理
        var userid = getUseridFomUsername(username);
        var photoURLs = getRecentPhotos(userid);
        for (int i = 0; i < PhotoFrames.Length; i++)
        {
            if (i >= photoURLs.Length) { break; }
            StartCoroutine(attacheWebImageToGameobject(photoURLs[i], PhotoFrames[i]));
        }
        return null;
    }



    /// <summary>
    /// InstagramユーザーネームからユーザーIDを取得する関数
    /// </summary>
    /// <param name="username">ユーザーネーム</param>
    /// <returns></returns>
    public string getUseridFomUsername(string username)
        {
            string API_URI = "https://api.instagram.com/v1/users/search?q={USERNAME}&access_token={ACCESS_TOKEN}";
            API_URI = API_URI.Replace("{USERNAME}", username);
            API_URI = API_URI.Replace("{ACCESS_TOKEN}", this.access_token);
            var jsonText = getHtml(API_URI);
            var json = JsonNode.Parse(jsonText);
            var id = json["data"][0]["id"].Get<string>();
            return (id);
        }


        /// <summary>
        /// 指定のユーザーの最新投稿写真のURLを配列で取得します。動画の場合はサムネイル画像になります。
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public string[] getRecentPhotos(string userid, int count = 20)
        {
            List<string>PhotoURLs= new List<string>();

            string API_URI = "https://api.instagram.com/v1/users/{USERID}/media/recent/?access_token={ACCESS_TOKEN}&count={COUNT}";
            API_URI = API_URI.Replace("{USERID}", userid);
            API_URI = API_URI.Replace("{ACCESS_TOKEN}", this.access_token);
            API_URI = API_URI.Replace("{COUNT}", count.ToString());
            var jsonText = getHtml(API_URI);
            var json = JsonNode.Parse(jsonText);
            var data = json["data"];
            foreach(var d in data)
            {
                var PhotoURL = d["images"]["standard_resolution"]["url"].Get<string>();
                PhotoURLs.Add(PhotoURL);
            }
            return (PhotoURLs.ToArray());
        }

    /// <summary>
    /// //指定したウェブ画像を読み込んでゲームオブジェクトのテクスチャとして表示
    /// 呼び出し方：StartCoroutine(attacheWebImageToGameobject(PhotoURL, Gameobject));
    /// </summary>
    /// <param name="url"></param>
    /// <param name="gObj"></param>
    /// <returns></returns>
    private IEnumerator<WWW> attacheWebImageToGameobject(string url, GameObject gObj)
        {
        WWW texturewww = new WWW(url);
        yield return texturewww;
        gObj.GetComponent<Renderer>().material.mainTexture = texturewww.texture;
        }





        /// <summary>
        /// HTMLの取得関数（SSLエラー対処済み）
        /// </summary>
        /// <param name="URL"></param>
        /// <returns></returns>
        private string getHtml(string URL)
        {
            SSLValidator.OverrideValidation();//Avoid SSL error
            WebClient wc = new WebClient();
            Stream st = wc.OpenRead(URL);
            Encoding enc = Encoding.GetEncoding("utf-8");
            StreamReader sr = new StreamReader(st, enc);
            string html = sr.ReadToEnd();
            sr.Close();
            st.Close();
            return (html);
        }

    }

    //Avoid SSL error
    //http://stackoverflow.com/questions/18454292/system-net-certificatepolicy-to-servercertificatevalidationcallback-accept-all-c
    public static class SSLValidator
    {
        private static bool OnValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        public static void OverrideValidation()
        {
            ServicePointManager.ServerCertificateValidationCallback = OnValidateCertificate;
            ServicePointManager.Expect100Continue = true;
        }
    }


    //【Unity】指定したゲームオブジェクトから名前で子オブジェクトを検索する拡張メソッド
    //http://baba-s.hatenablog.com/entry/2014/08/01/101104
    public static class GameObjectExtensions
    {
        /// <summary>
        /// 深い階層まで子オブジェクトを名前で検索して GameObject 型で取得します
        /// </summary>
        /// <param name="self">GameObject 型のインスタンス</param>
        /// <param name="name">検索するオブジェクトの名前</param>
        /// <param name="includeInactive">非アクティブなオブジェクトも検索する場合 true</param>
        /// <returns>子オブジェクト</returns>
        public static GameObject FindDeep(
            this GameObject self,
            string name,
            bool includeInactive = false)
        {
            var children = self.GetComponentsInChildren<Transform>(includeInactive);
            foreach (var transform in children)
            {
                if (transform.name == name)
                {
                    return transform.gameObject;
                }
            }
            return null;
        }
    }