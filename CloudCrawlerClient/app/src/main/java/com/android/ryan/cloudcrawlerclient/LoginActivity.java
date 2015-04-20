package com.android.ryan.cloudcrawlerclient;

import android.annotation.TargetApi;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Build;
import android.support.v7.app.ActionBarActivity;
import android.os.Bundle;
import android.util.Log;
import android.util.Xml;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.os.AsyncTask;
import android.widget.TextView;
import android.widget.Toast;

import com.google.gson.Gson;

import org.json.JSONObject;


public class LoginActivity extends ActionBarActivity {

    EditText enteredUsername, enteredPassword;
    Button btnLogin, btnCreateAccount;
    Context context;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_login);
        context = this;

        if(AccessState.instance().userIsLoggedIn(context)) {
            startActivity(new Intent(LoginActivity.this, MainActivity.class));
        }

        enteredUsername = (EditText)findViewById(R.id.et_username);
        enteredPassword = (EditText)findViewById(R.id.et_password);
        btnLogin = (Button)findViewById(R.id.btn_Login);
        btnCreateAccount = (Button)findViewById(R.id.btn_Create_Account);

        btnLogin.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                String username = enteredUsername.getText().toString();
                String password = enteredPassword.getText().toString();
                new AsyncLogin().execute(username, password);
            }
        });

        btnCreateAccount.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                Intent intent = new Intent(LoginActivity.this, CreateUserActivity.class);
                startActivity(intent);
            }
        });
    }


    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.menu_login, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        // Handle action bar item clicks here. The action bar will
        // automatically handle clicks on the Home/Up button, so long
        // as you specify a parent activity in AndroidManifest.xml.
        int id = item.getItemId();

        //noinspection SimplifiableIfStatement
        if (id == R.id.action_settings) {
            return true;
        }

        return super.onOptionsItemSelected(item);
    }

    public void setLoggedInState(String username){
        AccessState.instance().setUserLoggedIn(context, username);
        Intent intent = new Intent(LoginActivity.this, MainActivity.class);
        intent.putExtra("username", username);
        startActivity(intent);
    }

    protected class AsyncLogin extends AsyncTask<String, JSONObject, Boolean>{

        String username = null;

        @Override
        protected Boolean doInBackground(String... params){
            RestAPI api = new RestAPI();
            boolean userAuth = false;
            try{
                JSONObject jsonObj = api.UserAuthorization(params[0], params[1]);
                JSONParser parser = new JSONParser();
                userAuth = parser.parseUserAuth(jsonObj);
                username = params[0];

            } catch (Exception ex) {
                Log.d("AsyncLogin", ex.getMessage());
                ex.printStackTrace();
            }
            return userAuth;
        }

        @Override
        protected void onPreExecute(){
            super.onPreExecute();
            Toast.makeText(context, "Please Wait...", Toast.LENGTH_SHORT).show();
        }

        @Override
        protected void onPostExecute(Boolean result){
            if(result){
                setLoggedInState(username);
            } else {
                Toast.makeText(context, "Invalid username/password", Toast.LENGTH_SHORT).show();
            }
        }
    }
}
