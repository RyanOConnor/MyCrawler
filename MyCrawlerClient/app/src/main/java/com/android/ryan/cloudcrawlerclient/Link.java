package com.android.ryan.cloudcrawlerclient;

import java.io.Serializable;

/**
 * Created by Ryan on 4/26/2015.
 */
public class Link implements Serializable{

    String url;
    String innerText;
    int pageRank;

    public Link(String url, String innerText, int pageRank){
        this.url = url;
        this.innerText = innerText;
        this.pageRank = pageRank;
    }

    @Override
    public String toString() {
        return innerText;
    }
}
