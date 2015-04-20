package com.android.ryan.cloudcrawlerclient;

/**
 * Created by Ryan on 4/15/2015.
 */
public class UserDetailsTable {
    String username, password;

    public UserDetailsTable(String username, String password) {
        super();
        this.username = username;
        this.password = password;
    }

    public UserDetailsTable(){
        super();
        this.username = null;
        this.password = null;
    }

    public String getUsername() {
        return username;
    }

    public String getPassword(){
        return password;
    }

    public void setUsername(String username) {
        this.username = username;
    }

    public void setPassword(String password) {
        this.password = password;
    }
}
