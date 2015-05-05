package com.android.ryan.cloudcrawlerclient;

import java.io.Serializable;
import java.util.HashSet;
import java.util.List;

/**
 * Created by Ryan on 4/24/2015.
 */
public class FeedResults extends LinkFeed implements Serializable {

    private String userid;
    private String recordid;
    private String resultsid;

    public FeedResults(String url, HashSet<String> keywords, String htmlTags,
                       List<Link> userPageRank, String recordid, String resultsid){
        super(url, htmlTags,keywords,userPageRank);

        this.recordid = recordid;
        this.resultsid = resultsid;
    }

    public void addUserId(String userid){
        if(this.userid == null){
            this.userid = userid;
        }
    }

    public String getUserId(){
        return userid;
    }

    public String getRecordId() {
        return recordid;
    }

    public String getResultsId(){
        return resultsid;
    }

}
