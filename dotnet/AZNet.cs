/**
 * Copyright (C) <2014~>  <Lee Yonghun, metalsm7@gmail.com, visit http://azlib.mparang.com/>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
#if NETCORE1_0 || NET40 || NET452
using System;
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
                string rtnValue = "";
#if NET40
                try {
                    System.Net.HttpWebRequest myRequest = (System.Net.HttpWebRequest)WebRequest.Create(url);
                    myRequest.Method = "GET";
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