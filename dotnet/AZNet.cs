#if NETCORE1_0 || NET40 || NET452
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

            public string ReadSync(string url) {
                return ReadSync(url, "GET", null);
            }

            public string ReadSync(string url, string method, AZData data) {
                return ReadSync(url, method, null, data);
            }

            public string ReadSync(string url, string method, string content_type, AZData data) {
                string rtnValue = "";
#if NET40
                try {
                    System.Net.HttpWebRequest myRequest = (System.Net.HttpWebRequest)WebRequest.Create(url);
                    myRequest.Method = method;
                    if (content_type != null) myRequest.ContentType = content_type;
                    if (data != null) {
                        StringBuilder param = new StringBuilder();
                        for (int cnti=0; cnti<data.Size(); cnti++) {
                            param.AppendFormat("{0}{1}={2}", cnti > 0 ? "&" : "", data.GetKey(cnti), data.GetString(cnti));
                        }
                        byte[] param_byte = Encoding.UTF8.GetBytes(param.ToString());
                        myRequest.GetRequestStream().Write(param_byte, 0, param_byte.Length);
                    }
                    System.Net.HttpWebResponse myResponse = (System.Net.HttpWebResponse)myRequest.GetResponse();
                    System.IO.StreamReader reader = new System.IO.StreamReader(myResponse.GetResponseStream());

                    rtnValue = reader.ReadToEnd();

                    reader.Close();
                    reader.Dispose();
                    myResponse.Close();
                    myRequest = null;
                }
                catch (Exception ex) {
                    if (ex.InnerException != null) {
                        throw new Exception("Exception in GetHTML" + ex.ToString() + " / " + ex.InnerException.ToString(), ex);
                    }
                    else {
                        throw new Exception("Exception in GetHTML" + ex.ToString(), ex);
                    }
                }
#endif
                return rtnValue;
            }

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