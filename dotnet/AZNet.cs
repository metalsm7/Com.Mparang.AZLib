#if NET_STD || NET_CORE || NET_FX || NET_STORE
using System;
using System.Text;
using System.Threading;
using System.Net;

namespace Com.Mparang.AZLib {
  public class AZNet {
    public class Http {
        
      private ManualResetEvent allDone;

      public Http() {
        allDone = new ManualResetEvent(false);
      }

      /*
      public string ReadSync(string url) {
        return ReadSync(url, "GET", null);
      }
      public string ReadSync(string url, string method, AZData data) {
        return ReadSync(url, method, null, data);
      }
      */

      public void ReadAsync(string pUrl, Action<string> pOnSuccess) {
        ReadAsync(pUrl, pOnSuccess, null, System.Text.Encoding.UTF8);
      }

      public void ReadAsync(string pUrl, Action<string> pOnSuccess, Action<Exception> pOnError) {
        ReadAsync(pUrl, pOnSuccess, pOnError, System.Text.Encoding.UTF8);
      }

      public void ReadAsync(string pUrl, Action<string> pOnSuccess, Action<Exception> pOnError, System.Text.Encoding pEncoding) {
        //Console.Write("url:" + pUrl);
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(pUrl);
        request.ContentType = "application/x-www-form-urlencoded";
        request.Method = "GET";
        request.BeginGetResponse(new AsyncCallback(
          (pResult) => {
            string data = "";
            try {
              HttpWebRequest myrequest = (HttpWebRequest)pResult.AsyncState;
              using (HttpWebResponse response = (HttpWebResponse)myrequest.EndGetResponse(pResult)) {
                System.IO.Stream responseStream = response.GetResponseStream();
                using (var reader = new System.IO.StreamReader(responseStream, pEncoding)) {
                  data = reader.ReadToEnd();
                }
                responseStream.Dispose();
              }
              pOnSuccess(data);
            }
            catch (Exception e) {
              if (pOnError != null) {
                pOnError(e);
              }
              //throw;
            }
            finally {
              allDone.Set();
            }
          }
        ), request);

        allDone.WaitOne();
      }
    }
  }
}
#endif