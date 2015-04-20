package com.android.ryan.cloudcrawlerclient;

import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.AsyncTask;
import android.support.v7.app.ActionBarActivity;
import android.os.Bundle;
import android.util.Log;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;

import org.json.JSONObject;


public class CreateUserActivity extends ActionBarActivity {

    EditText enteredUsername, enteredPassword;
    Button btnCreateUser;
    Context context;

    @Override
    protected void onCreate(Bundle savedInstanceState){
        super.onCreate(savedInstanceState);
        context = this;

        setContentView(R.layout.activity_create_user);
        enteredUsername = (EditText)findViewById(R.id.et_username);
        enteredPassword = (EditText)findViewById(R.id.et_password);
        btnCreateUser = (Button)findViewById(R.id.btn_createuser);

        btnCreateUser.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                String username, password;

                username = enteredUsername.getText().toString();
                password = enteredPassword.getText().toString();

                UserDetailsTable userDetails = new UserDetailsTable(username, password);
                new AsyncCreateUser().execute(userDetails);
            }
        });
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.menu_create_user, menu);
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
        AccessState.instance().setUserLoggedIn(this.context, username);
        Intent intent = new Intent(CreateUserActivity.this, MainActivity.class);
        startActivity(intent);
    }

    protected class AsyncCreateUser extends AsyncTask<UserDetailsTable, Void, RestAPI.ServerResponse> {

        String username, password;

        @Override
        protected RestAPI.ServerResponse doInBackground(UserDetailsTable... params) {
            username = params[0].getUsername();
            password = params[0].getPassword();
            RestAPI api = new RestAPI();
            RestAPI.ServerResponse response = RestAPI.ServerResponse.ServerError;
            try{
                JSONObject obj = api.CreateNewAccount(username, password);
                JSONParser parser = new JSONParser();
                response = parser.parseServerResponse(obj);

            } catch(Exception ex) {
                Log.d("AsyncCreateUser", ex.getMessage());
            }
            return response;
        }

        @Override
        protected void onPostExecute(RestAPI.ServerResponse response) {
            switch(response) {
                case Success:
                    setLoggedInState(username);
                    break;
                case UsernameAlreadyExists:
                    Toast.makeText(context, "Username Already Exists", Toast.LENGTH_SHORT).show();
                    break;
                case InvalidPassword:
                    Toast.makeText(context, "Invalid Password", Toast.LENGTH_SHORT).show();
                    break;
                case ServerError:
                    Toast.makeText(context, "Server Error", Toast.LENGTH_SHORT).show();
            }
        }
    }
}
