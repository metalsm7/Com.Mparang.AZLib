#if NET40 || NET452
using System;
using System.Text;
using System.Web;
using static System.Web.HttpContext;
using System.Net;

namespace Com.Mparang.AZLib {
    public static class AZCookie {
        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public enum Encrypt {
            PLAIN, BASE64, AES256
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public static void Set(string p_key, string p_value, string p_domain, int? p_remain_days, Encrypt? p_encrypt, string p_encrypt_key) {
            string sub_key = null;
            if (p_key.IndexOf(".") > 0) {
                sub_key = p_key.Substring(p_key.IndexOf(".") + 1);
                p_key = p_key.Substring(0, p_key.IndexOf("."));
            }

            Set(p_key, sub_key, p_value, p_domain, p_remain_days, p_encrypt, p_encrypt_key);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public static void Set(string p_key, string p_sub_key, string p_value, string p_domain, int? p_remain_days, Encrypt? p_encrypt, string p_encrypt_key) {
            //
            string value = "";
            HttpContext context = HttpContext.Current;
            Encrypt encrypt = Encrypt.BASE64;
            int remain_days = 0;

            //
            if (p_encrypt.HasValue) {
                encrypt = p_encrypt.Value;
            }

            p_key = context.Server.UrlEncode(p_key);

            if (p_sub_key != null) {
                string cookieValue = Get(p_key, p_encrypt, p_encrypt_key);
                if (cookieValue == null || cookieValue.Trim().Length < 1) cookieValue = "";
                char[] delim_cookieValues = { '&' };
                string[] cookieValues = cookieValue.Split(delim_cookieValues);
                bool existChk = false;
                for (int cnti = 0; cnti < cookieValues.Length; cnti++) {
                    if (cookieValues[cnti].IndexOf(p_sub_key + "=") == 0) {
                        cookieValues[cnti] = p_sub_key + "=" + p_value;
                        existChk = true;
                    }
                    if (cnti > 0) value += "&";
                    value += cookieValues[cnti];
                }
                if (!existChk) {
                    if (value.Trim().Length > 0) value += "&";
                    value += p_sub_key + "=" + p_value;
                }
            }
            else {
                value = p_value;
            }

            //
            switch (encrypt) {
                case Encrypt.PLAIN:
                    break;
                case Encrypt.BASE64:    // BASE64 인코딩된 자료에 대한 반환 처리
                    value = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(HttpUtility.UrlEncode(value)));
                    break;
                case Encrypt.AES256:    // AES256 인코딩된 자료에 대한 반환 처리
                    value = new AZEncrypt.AES256().Enc(value, p_encrypt_key);
                    //net.imcore.AES256Cipher aes = new net.imcore.AES256Cipher();
                    //value = aes.Encode(value, p_encrypt_key);
                    //aes = null;
                    break;
            }

            //
            HttpCookie cookies = new HttpCookie(p_key);
            cookies.Value = value;

            //
            if (p_domain != null) {
                cookies.Domain = p_domain;
            }

            //
            if (p_remain_days.HasValue) {
                remain_days = p_remain_days.Value;
            }
            if (remain_days != 0) cookies.Expires = DateTime.Now.AddDays(remain_days);
            context.Response.Cookies.Set(cookies);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public static void Set<T>(T p_model) {
            Set<T>(null, p_model, null, null, null, null);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public static void Set<T>(T p_model, string p_domain) {
            Set<T>(null, p_model, p_domain, null, null, null);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public static void Set<T>(T p_model, string p_domain, Encrypt? p_encrypt, string p_encrypt_key) {
            Set<T>(null, p_model, p_domain, null, p_encrypt, p_encrypt_key);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public static void Set<T>(string p_key, T p_model, string p_domain, int? p_remain_days, Encrypt? p_encrypt, string p_encrypt_key) {
            string key, value = "";
            Encrypt encrypt = Encrypt.BASE64;
            int remain_days = 0;

            //
            HttpContext context = System.Web.HttpContext.Current;

            // 쿠키 최상위 이름값이 없으면 모델의 이름으로 지정
            key = p_key != null ? p_key : p_model.GetType().Name;
            key = context.Server.UrlEncode(key);

            // T -> AZData 형식으로 변환
            AZData data = AZString.JSON.Init(AZString.JSON.Convert<T>(p_model)).ToAZData();

            for (int cnti = 0; cnti < data.Size(); cnti++) {
                value += (cnti > 0 ? "&" : "") + data.GetKey(cnti) + "=" + context.Server.UrlEncode(data.GetString(cnti));
            }

            //
            if (p_encrypt.HasValue) {
                encrypt = p_encrypt.Value;
            }

            //
            switch (encrypt) {
                case Encrypt.PLAIN:
                    break;
                case Encrypt.BASE64:    // BASE64 인코딩된 자료에 대한 반환 처리
                    value = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(HttpUtility.UrlEncode(value)));
                    break;
                case Encrypt.AES256:    // AES256 인코딩된 자료에 대한 반환 처리
                    value = new AZEncrypt.AES256().Enc(value, p_encrypt_key);
                    /*net.imcore.AES256Cipher aes = new net.imcore.AES256Cipher();
                    value = aes.Encode(value, p_encrypt_key);
                    aes = null;*/
                    break;
            }

            //
            HttpCookie cookies = new HttpCookie(key);
            cookies.Value = value;

            //
            if (p_domain != null) {
                cookies.Domain = p_domain;
            }

            //
            if (p_remain_days.HasValue) {
                remain_days = p_remain_days.Value;
            }
            if (remain_days != 0) cookies.Expires = DateTime.Now.AddDays(remain_days);
            context.Response.Cookies.Set(cookies);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public static T Get<T>() {
            return Get<T>(null, null, null);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public static T Get<T>(Encrypt? p_encrypt, string p_encrypt_key) {
            return Get<T>(null, p_encrypt, p_encrypt_key);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public static T Get<T>(string p_key, Encrypt? p_encrypt, string p_encrypt_key) {
            Type type = typeof(T);
            object model = Activator.CreateInstance(type);

            T rtn_value = default(T);
            string key, value;
            Encrypt encrypt = Encrypt.BASE64;

            //
            HttpContext context = System.Web.HttpContext.Current;

            // 쿠키 최상위 이름값이 없으면 모델의 이름으로 지정
            key = p_key != null ? p_key : model.GetType().Name;
            
            if (p_encrypt.HasValue) {
                encrypt = p_encrypt.Value;
            }

            // T -> AZData 형식으로 변환
            AZData data = AZString.JSON.Init(AZString.JSON.Convert<T>((T)model)).ToAZData();
            model = null;

            //
            value = Get(key, encrypt, p_encrypt_key);
            if (value != null) {
                for (int cnti = 0; cnti < data.Size(); cnti++) {
                    string sub_value = GetSubValue(data.GetKey(cnti), value);
                    if (sub_value != null) {
                        data.Set(cnti, sub_value);
                    }
                }
                rtn_value = data.Convert<T>();
            }

            return rtn_value;
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public static string Get(string p_key, Encrypt? p_encrypt, string p_encrypt_key) {
            string sub_key = null;
            if (p_key.IndexOf(".") > 0) {
                sub_key = p_key.Substring(p_key.IndexOf(".") + 1);
                p_key = p_key.Substring(0, p_key.IndexOf("."));
            }

            return Get(p_key, sub_key, p_encrypt, p_encrypt_key);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public static string Get(string p_key, string p_sub_key, Encrypt? p_encrypt, string p_encrypt_key) {
            string rtn_value = null;
            string value = "";
            Encrypt encrypt = Encrypt.BASE64;

            //
            HttpContext context = System.Web.HttpContext.Current;

            //
            p_key = context.Server.UrlEncode(p_key);

            //
            if (HasKey(p_key)) {
                value = context.Request.Cookies[p_key].Value;
            }

            //
            if (p_encrypt.HasValue) {
                encrypt = p_encrypt.Value;
            }

            switch (encrypt) {
                case Encrypt.PLAIN:
                    break;
                case Encrypt.BASE64:    // BASE64 인코딩된 자료에 대한 반환 처리
                    try {
                        value = HttpUtility.UrlDecode(Encoding.UTF8.GetString(Convert.FromBase64String(context.Request.Cookies[p_key].Value)));
                    }
                    catch (Exception) {
                        value = "";
                    }
                    break;
                case Encrypt.AES256:    // AES256 인코딩된 자료에 대한 반환 처리
                    try {
                    value = new AZEncrypt.AES256().Enc(value, p_encrypt_key);
                        /*net.imcore.AES256Cipher aes = new net.imcore.AES256Cipher();
                        value = aes.Decode(value, p_encrypt_key);
                        aes = null;*/
                    }
                    catch (Exception) {
                        value = "";
                    }
                    break;
            }

            //
            if (p_sub_key != null && p_sub_key.Trim().Length > 0) {
                rtn_value = GetSubValue(p_sub_key, value);
            }
            else {
                rtn_value = value;
            }

            return rtn_value;
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public static void Remove(string p_key) {
            HttpContext context = System.Web.HttpContext.Current;
            if (context.Response.Cookies[p_key] != null) {
                context.Response.Cookies[p_key].Value = HttpUtility.UrlEncode("");
                context.Response.Cookies[p_key].Expires = DateTime.Now.AddDays(-1);
            }
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public static void Clear() {
            HttpContext context = System.Web.HttpContext.Current;
            for (int cnti = 0; cnti < context.Response.Cookies.Count; cnti++) {
                context.Response.Cookies.Get(cnti).Value = HttpUtility.UrlEncode("");
                context.Response.Cookies.Get(cnti).Expires = DateTime.Now.AddDays(-1);
            }
            context.Response.Cookies.Clear();
        }


        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public static bool HasKey(string p_key) {
            bool rtn_value = false;
            //
            HttpContext context = System.Web.HttpContext.Current;

            for (int cnti = 0; cnti < context.Request.Cookies.Count; cnti++) {
                if (context.Request.Cookies[cnti].Name.Equals(p_key)) {
                    rtn_value = true;
                    break;
                }
            }
            return rtn_value;
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        private static string GetSubValue(string p_sub_key, string p_value) {
            string rtn_value = null;
            //
            HttpContext context = System.Web.HttpContext.Current;

            bool existChk = false;
            string[] sub_cookies = p_value.Split(new char[] { '&' });
            for (int cnti = 0; cnti < sub_cookies.Length; cnti++) {
                if (sub_cookies[cnti].IndexOf(p_sub_key + "=") == 0) {
                    rtn_value = context.Server.UrlDecode(sub_cookies[cnti].Substring(sub_cookies[cnti].IndexOf(p_sub_key + "=") + (p_sub_key + "=").Length));
                    existChk = true;
                    break;
                }
            }
            if (!existChk) rtn_value = null;

            return rtn_value;
        }
    }
}
#endif
#if NETCORE1_0
using System;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;

namespace Com.Mparang.AZLib {
    public class AZCookie {
        private HttpContext context;
        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public enum Encrypt {
            PLAIN, BASE64, AES256
        }

        public AZCookie(HttpContext p_context) {
            context = p_context;
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public void Set(string p_key, string p_value, string p_domain, int? p_remain_days, Encrypt? p_encrypt, string p_encrypt_key) {
            string sub_key = null;
            if (p_key.IndexOf(".") > 0) {
                sub_key = p_key.Substring(p_key.IndexOf(".") + 1);
                p_key = p_key.Substring(0, p_key.IndexOf("."));
            }

            Set(p_key, sub_key, p_value, p_domain, p_remain_days, p_encrypt, p_encrypt_key);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public void Set(string p_key, string p_sub_key, string p_value, string p_domain, int? p_remain_days, Encrypt? p_encrypt, string p_encrypt_key) {
            //
            string value = "";
            //HttpContext context = System.Web.HttpContext.Current;
            Encrypt encrypt = Encrypt.BASE64;
            int remain_days = 0;

            //
            if (p_encrypt.HasValue) {
                encrypt = p_encrypt.Value;
            }

            //p_key = context.Server.UrlEncode(p_key);
            p_key = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(p_key));

            if (p_sub_key != null) {
                string cookieValue = Get(p_key, p_encrypt, p_encrypt_key);
                if (cookieValue == null || cookieValue.Trim().Length < 1) cookieValue = "";
                char[] delim_cookieValues = { '&' };
                string[] cookieValues = cookieValue.Split(delim_cookieValues);
                bool existChk = false;
                for (int cnti = 0; cnti < cookieValues.Length; cnti++) {
                    if (cookieValues[cnti].IndexOf(p_sub_key + "=") == 0) {
                        cookieValues[cnti] = p_sub_key + "=" + p_value;
                        existChk = true;
                    }
                    if (cnti > 0) value += "&";
                    value += cookieValues[cnti];
                }
                if (!existChk) {
                    if (value.Trim().Length > 0) value += "&";
                    value += p_sub_key + "=" + p_value;
                }
            }
            else {
                value = p_value;
            }

            //
            switch (encrypt) {
                case Encrypt.PLAIN:
                    break;
                case Encrypt.BASE64:    // BASE64 인코딩된 자료에 대한 반환 처리
                    value = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(UrlEncoder.Default.Encode(value)));
                    break;
                case Encrypt.AES256:    // AES256 인코딩된 자료에 대한 반환 처리
                    //net.imcore.AES256Cipher aes = new net.imcore.AES256Cipher();
                    value = new AZEncrypt.AES256().Enc(value, p_encrypt_key);//aes.Encode(value, p_encrypt_key);
                    //aes = null;
                    break;
            }

            //
            /*HttpCookie cookies = new HttpCookie(p_key);
            cookies.Value = value;*/

            //
            /*if (p_domain != null) {
                cookies.Domain = p_domain;
            }*/

            //
            if (p_remain_days.HasValue) {
                remain_days = p_remain_days.Value;
            }
            //cookies.Expires = DateTime.Now.AddDays(remain_days);
            //context.Response.Cookies.Set(cookies);

            //
            Microsoft.AspNetCore.Http.CookieOptions options = new Microsoft.AspNetCore.Http.CookieOptions();
            options.Expires = DateTime.Now.AddDays(remain_days);
            if (p_domain != null) {
                options.Domain = p_domain;
            }
            context.Response.Cookies.Delete(p_key);
            context.Response.Cookies.Append(p_key, value, options);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public void Set<T>(T p_model) {
            Set<T>(null, p_model, null, null, null, null);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public void Set<T>(T p_model, string p_domain) {
            Set<T>(null, p_model, p_domain, null, null, null);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public void Set<T>(T p_model, string p_domain, Encrypt? p_encrypt, string p_encrypt_key) {
            Set<T>(null, p_model, p_domain, null, p_encrypt, p_encrypt_key);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public void Set<T>(string p_key, T p_model, string p_domain, int? p_remain_days, Encrypt? p_encrypt, string p_encrypt_key) {
            string key, value = "";
            Encrypt encrypt = Encrypt.BASE64;
            int remain_days = 0;

            //
            //HttpContext context = System.Web.HttpContext.Current;

            // 쿠키 최상위 이름값이 없으면 모델의 이름으로 지정
            key = p_key != null ? p_key : p_model.GetType().Name;
            key = UrlEncoder.Default.Encode(key);//context.Server.UrlEncode(key);

            // T -> AZData 형식으로 변환
            AZData data = AZString.JSON.Init(AZString.JSON.Convert<T>(p_model)).ToAZData();

            for (int cnti = 0; cnti < data.Size(); cnti++) {
                value += (cnti > 0 ? "&" : "") + data.GetKey(cnti) + "=" + UrlEncoder.Default.Encode(data.GetString(cnti));//context.Server.UrlEncode(data.GetString(cnti));
            }

            //
            if (p_encrypt.HasValue) {
                encrypt = p_encrypt.Value;
            }

            //
            switch (encrypt) {
                case Encrypt.PLAIN:
                    break;
                case Encrypt.BASE64:    // BASE64 인코딩된 자료에 대한 반환 처리
                    value = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(UrlEncoder.Default.Encode(value)));
                    break;
                case Encrypt.AES256:    // AES256 인코딩된 자료에 대한 반환 처리
                    //net.imcore.AES256Cipher aes = new net.imcore.AES256Cipher();
                    value = new AZEncrypt.AES256().Enc(value, p_encrypt_key);//aes.Encode(value, p_encrypt_key);
                    //aes = null;
                    break;
            }

            //
            //HttpCookie cookies = new HttpCookie(key);
            //cookies.Value = value;

            //
            /*if (p_domain != null) {
                cookies.Domain = p_domain;
            }*/

            //
            if (p_remain_days.HasValue) {
                remain_days = p_remain_days.Value;
            }

            Microsoft.AspNetCore.Http.CookieOptions options = new Microsoft.AspNetCore.Http.CookieOptions();
            options.Expires = DateTime.Now.AddDays(remain_days);
            if (p_domain != null) {
                options.Domain = p_domain;
            }
            context.Response.Cookies.Delete(p_key);
            context.Response.Cookies.Append(p_key, value, options);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public T Get<T>() {
            return Get<T>(null, null, null);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public T Get<T>(Encrypt? p_encrypt, string p_encrypt_key) {
            return Get<T>(null, p_encrypt, p_encrypt_key);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public T Get<T>(string p_key, Encrypt? p_encrypt, string p_encrypt_key) {
            Type type = typeof(T);
            object model = Activator.CreateInstance(type);

            T rtn_value = default(T);
            string key, value;
            Encrypt encrypt = Encrypt.BASE64;

            //
            //HttpContext context = System.Web.HttpContext.Current;

            // 쿠키 최상위 이름값이 없으면 모델의 이름으로 지정
            key = p_key != null ? p_key : model.GetType().Name;
            
            if (p_encrypt.HasValue) {
                encrypt = p_encrypt.Value;
            }

            // T -> AZData 형식으로 변환
            AZData data = AZString.JSON.Init(AZString.JSON.Convert<T>((T)model)).ToAZData();
            model = null;

            //
            value = Get(key, encrypt, p_encrypt_key);
            if (value != null) {
                for (int cnti = 0; cnti < data.Size(); cnti++) {
                    string sub_value = GetSubValue(data.GetKey(cnti), value);
                    if (sub_value != null) {
                        data.Set(cnti, sub_value);
                    }
                }
                rtn_value = data.Convert<T>();
            }

            return rtn_value;
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public string Get(string p_key, Encrypt? p_encrypt, string p_encrypt_key) {
            string sub_key = null;
            if (p_key.IndexOf(".") > 0) {
                sub_key = p_key.Substring(p_key.IndexOf(".") + 1);
                p_key = p_key.Substring(0, p_key.IndexOf("."));
            }

            return Get(p_key, sub_key, p_encrypt, p_encrypt_key);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public string Get(string p_key, string p_sub_key, Encrypt? p_encrypt, string p_encrypt_key) {
            string rtn_value = null;
            string value = "";
            Encrypt encrypt = Encrypt.BASE64;

            //
            //HttpContext context = System.Web.HttpContext.Current;

            //
            p_key = UrlEncoder.Default.Encode(p_key);

            //
            if (HasKey(p_key)) {
                value = context.Request.Cookies[p_key];//context.Request.Cookies[p_key].Value;
            }

            //
            if (p_encrypt.HasValue) {
                encrypt = p_encrypt.Value;
            }

            switch (encrypt) {
                case Encrypt.PLAIN:
                    break;
                case Encrypt.BASE64:    // BASE64 인코딩된 자료에 대한 반환 처리
                    try {
                        value = Encoding.UTF8.GetString(Convert.FromBase64String(context.Request.Cookies[p_key]));
                    }
                    catch (Exception) {
                        value = "";
                    }
                    break;
                case Encrypt.AES256:    // AES256 인코딩된 자료에 대한 반환 처리
                    try {
                        //net.imcore.AES256Cipher aes = new net.imcore.AES256Cipher();
                        value = new AZEncrypt.AES256().Dec(value, p_encrypt_key);//aes.Decode(value, p_encrypt_key);
                        //aes = null;
                    }
                    catch (Exception) {
                        value = "";
                    }
                    break;
            }

            //
            if (p_sub_key != null && p_sub_key.Trim().Length > 0) {
                rtn_value = GetSubValue(p_sub_key, value);
            }
            else {
                rtn_value = value;
            }

            return rtn_value;
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public void Remove(string p_key) {
            context.Response.Cookies.Delete(p_key);
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public void Clear() {
            //HttpContext context = System.Web.HttpContext.Current;
            foreach (var key in context.Request.Cookies.Keys) {
            //for (int cnti = 0; cnti < context.Request.Cookies.Count; cnti++) {
                context.Response.Cookies.Delete(key);
            }
            //context.Response.Cookies.Clear();
        }


        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        public bool HasKey(string p_key) {
            bool rtn_value = false;
            //
            //HttpContext context = System.Web.HttpContext.Current;

            for (int cnti = 0; cnti < context.Request.Cookies.Count; cnti++) {
                if (context.Request.Cookies.ContainsKey(p_key)) {
                    rtn_value = true;
                    break;
                }
            }
            return rtn_value;
        }

        /**
         * <summary></summary>
         * Created in 2015-07-29, leeyonghun
         */
        private string GetSubValue(string p_sub_key, string p_value) {
            string rtn_value = null;
            //
            //HttpContext context = System.Web.HttpContext.Current;

            bool existChk = false;
            string[] sub_cookies = p_value.Split(new char[] { '&' });
            for (int cnti = 0; cnti < sub_cookies.Length; cnti++) {
                if (sub_cookies[cnti].IndexOf(p_sub_key + "=") == 0) {
                    rtn_value = sub_cookies[cnti].Substring(sub_cookies[cnti].IndexOf(p_sub_key + "=") + (p_sub_key + "=").Length);
                    existChk = true;
                    break;
                }
            }
            if (!existChk) rtn_value = null;

            return rtn_value;
        }
    }

}
#endif