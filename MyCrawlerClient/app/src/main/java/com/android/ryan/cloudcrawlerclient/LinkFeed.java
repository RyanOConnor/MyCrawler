package com.android.ryan.cloudcrawlerclient;

import java.io.Serializable;
import java.util.HashSet;
import java.util.List;

/**
 * Created by Ryan on 5/3/2015.
 */
public class LinkFeed implements Serializable{

    String url;
    String htmlTags;
    HashSet<String> keywords;
    List<Link> userPageRank;

    public LinkFeed(String url, String htmlTags, HashSet<String> keywords, List<Link> userPageRank){
        this.url = url;
        this.htmlTags = htmlTags;
        this.keywords = keywords;
        this.userPageRank = userPageRank;
    }
}
