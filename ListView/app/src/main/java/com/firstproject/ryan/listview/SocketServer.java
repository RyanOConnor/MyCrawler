package com.firstproject.ryan.listview;

import android.os.Handler;
import android.os.Message;

import com.google.gson.Gson;

import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.io.IOException;
import java.io.ObjectInputStream;
import java.io.ObjectOutputStream;
import java.net.InetAddress;
import java.net.Socket;
import java.net.UnknownHostException;

/**
 * Created by Ryan on 4/13/2015.
 */
public class SocketServer {

    static Socket serverSocket;
    static Thread serverThread;

    public class Message {
        public String id;
    }

    public class UserMessage extends Message {
        public String userid;
        public String username;
    }

    public class CreateUser extends UserMessage {
        public String enteredUserName;
        public byte[] enteredPassword;

        public CreateUser(String userid, String username, String enteredUserName, byte[] password) {
            this.userid = userid;
            this.username = username;
            this.enteredUserName = enteredUserName;
            this.enteredPassword = password;
        }
    }

    public void Start() {
        new Thread(new ClientThread()).start();
    }

    public String Package(String message) {
        return "<BOF>" + message + "<EOF>";
    }

    public class ClientThread implements Runnable{
        @Override
        public void run() {
            try{
                InetAddress serverAddress = InetAddress.getByName("192.168.1.132");
                serverSocket = new Socket(serverAddress, 11002);

                CreateUser cu = new CreateUser("userid", "user", "eUser", ((String)"password").getBytes());
                Gson gson = new Gson();
                String message = Package(gson.toJson(cu));

                DataOutputStream dos = new DataOutputStream(serverSocket.getOutputStream());
                dos.writeUTF(message);


                DataInputStream dis = new DataInputStream(serverSocket.getInputStream());
                String str = dis.readUTF();



                //serverMessage.obj = str;

                //mHandler.sendMessage(serverMessage);
                dos.close();
                dis.close();
            } catch(UnknownHostException ex1){
                ex1.printStackTrace();
            } catch(IOException ex2) {
                ex2.printStackTrace();
            }
        }
    }

    Handler mHandler = new Handler() {
        //@Override
        public void handleMessage(Message msg) {
            //displayMessage(msg.obj.toString());
        }
    };

    public void displayMessage(String message){
        message = "";
        //message.setText("" + message);
    }
}
