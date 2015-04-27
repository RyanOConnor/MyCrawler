/* JSON API for CloudCrawler Android Client */
package com.android.ryan.cloudcrawlerclient;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.io.UnsupportedEncodingException;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;
import java.net.HttpURLConnection;
import java.net.URL;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Date;
import java.util.HashMap;
import java.util.Locale;
import java.util.Map;

public class RestAPI {

    public static enum ServerResponse {
        Success, ServerError, InvalidPasswordType, InvalidPassword, UsernameAlreadyExists, LinkAlreadyExists,
        DummyResponse
    };

    private final String urlString = "http://10.0.2.2/AndroidWebApi2/Handler.ashx";
    //private final String urlString = "http://10.0.2.2/TestApi1/Handler.ashx";

    private static String convertStreamToUTF8String(InputStream stream) throws IOException {
        String result = "";
        StringBuilder sb = new StringBuilder();
        try {
            InputStreamReader reader = new InputStreamReader(stream, "UTF-8");
            char[] buffer = new char[4096];
            int readChars = 0;
            while (readChars != -1) {
                readChars = reader.read(buffer);
                if (readChars > 0)
                    sb.append(buffer, 0, readChars);
            }
            result = sb.toString();
        } catch (UnsupportedEncodingException e) {
            e.printStackTrace();
        }
        return result;
    }


    private String load(String contents) throws IOException {
        URL url = new URL(urlString);
        HttpURLConnection conn = (HttpURLConnection)url.openConnection();
        conn.setRequestMethod("POST");
        conn.setConnectTimeout(60000);
        conn.setDoOutput(true);
        conn.setDoInput(true);
        OutputStreamWriter w = new OutputStreamWriter(conn.getOutputStream());
        w.write(contents);
        w.flush();
        InputStream istream = conn.getInputStream();
        String result = convertStreamToUTF8String(istream);
        return result;
    }

    private Object mapObject(Object o) {
        Object finalValue = null;
        if (o.getClass() == String.class) {
            finalValue = o;
        }
        else if (Number.class.isInstance(o)) {
            finalValue = String.valueOf(o);
        } else if (Date.class.isInstance(o)) {
            SimpleDateFormat sdf = new SimpleDateFormat("MM/dd/yyyy hh:mm:ss", new Locale("en", "USA"));
            finalValue = sdf.format((Date)o);
        }
        else if (Collection.class.isInstance(o)) {
            Collection<?> col = (Collection<?>) o;
            JSONArray jarray = new JSONArray();
            for (Object item : col) {
                jarray.put(mapObject(item));
            }
            finalValue = jarray;
        } else {
            Map<String, Object> map = new HashMap<String, Object>();
            Method[] methods = o.getClass().getMethods();
            for (Method method : methods) {
                if (method.getDeclaringClass() == o.getClass()
                        && method.getModifiers() == Modifier.PUBLIC
                        && method.getName().startsWith("get")) {
                    String key = method.getName().substring(3);
                    try {
                        Object obj = method.invoke(o, null);
                        Object value = mapObject(obj);
                        map.put(key, value);
                        finalValue = new JSONObject(map);
                    } catch (Exception e) {
                        e.printStackTrace();
                    }
                }
            }
        }
        return finalValue;
    }

    public JSONObject UserAuthorization(String username,String password) throws Exception {
        JSONObject result = null;
        JSONObject o = new JSONObject();
        JSONObject p = new JSONObject();
        o.put("interface","RestAPI");
        o.put("method", "UserAuthorization");
        p.put("username",mapObject(username));
        p.put("password",mapObject(password));
        o.put("parameters", p);
        String s = o.toString();
        String r = load(s);
        result = new JSONObject(r);
        return result;
    }

    public JSONObject CreateNewAccount(String username,String password) throws Exception {
        JSONObject result = null;
        JSONObject o = new JSONObject();
        JSONObject p = new JSONObject();
        o.put("interface","RestAPI");
        o.put("method", "CreateNewAccount");
        p.put("username",mapObject(username));
        p.put("password",mapObject(password));
        o.put("parameters", p);
        String s = o.toString();
        String r = load(s);
        result = new JSONObject(r);
        return result;
    }

    public JSONObject AddLinkFeed(String userid,String url,String htmlTags,Object keywords) throws Exception {
        JSONObject result = null;
        JSONObject o = new JSONObject();
        JSONObject p = new JSONObject();
        o.put("interface","RestAPI");
        o.put("method", "AddLinkFeed");
        p.put("userid",mapObject(userid));
        p.put("url",mapObject(url));
        p.put("htmlTags",mapObject(htmlTags));
        p.put("keywords",mapObject(keywords));
        o.put("parameters", p);
        String s = o.toString();
        String r = load(s);
        result = new JSONObject(r);
        return result;
    }

    public JSONObject ModifyFeed(Object modifiedEntry) throws Exception {
        JSONObject result = null;
        JSONObject o = new JSONObject();
        JSONObject p = new JSONObject();
        o.put("interface","RestAPI");
        o.put("method", "ModifyFeed");
        p.put("modifiedEntry",mapObject(modifiedEntry));
        o.put("parameters", p);
        String s = o.toString();
        String r = load(s);
        result = new JSONObject(r);
        return result;
    }

    public JSONObject RemoveItem(String itemid) throws Exception {
        JSONObject result = null;
        JSONObject o = new JSONObject();
        JSONObject p = new JSONObject();
        o.put("interface","RestAPI");
        o.put("method", "RemoveItem");
        p.put("itemid",mapObject(itemid));
        o.put("parameters", p);
        String s = o.toString();
        String r = load(s);
        result = new JSONObject(r);
        return result;
    }

    public JSONObject ChangePassword(String newPassword) throws Exception {
        JSONObject result = null;
        JSONObject o = new JSONObject();
        JSONObject p = new JSONObject();
        o.put("interface","RestAPI");
        o.put("method", "ChangePassword");
        p.put("newPassword",mapObject(newPassword));
        o.put("parameters", p);
        String s = o.toString();
        String r = load(s);
        result = new JSONObject(r);
        return result;
    }

    public JSONObject DeleteUser(String userid) throws Exception {
        JSONObject result = null;
        JSONObject o = new JSONObject();
        JSONObject p = new JSONObject();
        o.put("interface","RestAPI");
        o.put("method", "DeleteUser");
        p.put("userid",mapObject(userid));
        o.put("parameters", p);
        String s = o.toString();
        String r = load(s);
        result = new JSONObject(r);
        return result;
    }

    public JSONObject RetrieveUpdates(String userid) throws Exception {
        JSONObject result = null;
        JSONObject o = new JSONObject();
        JSONObject p = new JSONObject();
        o.put("interface","RestAPI");
        o.put("method", "RetrieveUpdates");
        p.put("userid",mapObject(userid));
        o.put("parameters", p);
        String s = o.toString();
        String r = load(s);
        result = new JSONObject(r);
        return result;
    }

}


