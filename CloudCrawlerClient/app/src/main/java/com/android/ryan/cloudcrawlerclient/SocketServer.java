package com.android.ryan.cloudcrawlerclient;

import com.google.gson.Gson;

import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.io.IOException;
import java.net.InetAddress;
import java.net.Socket;
import java.net.UnknownHostException;

/**
 * Created by Ryan on 4/14/2015.
 */
public class SocketServer {

    static Socket serverSocket;
    static Thread serverThread;

    public void Start() {
        new Thread(new ClientThread()).start();
    }

    public String Package(String message) {
        return "<BOF>" + message + "<EOF>";
    }

    public CreateUser generateDummyUser(){
        CreateUser newUser = new CreateUser("0j4j43j34j4", "54j4b6v5h4j", "ryguy90", "ryguy91", ((String)"p455w0rd").getBytes());
        return newUser;
    }

    public class ClientThread implements Runnable{
        @Override
        public void run() {
            try{
                InetAddress serverAddress = InetAddress.getByName("192.168.1.132");
                serverSocket = new Socket(serverAddress, 11002);

                CreateUser newUser = generateDummyUser();

                Gson gson = new Gson();
                String message = Package(gson.toJson(newUser));

                DataOutputStream dos = new DataOutputStream(serverSocket.getOutputStream());
                dos.writeUTF(message);

                DataInputStream dis = new DataInputStream(serverSocket.getInputStream());
                String str = dis.readUTF();

                dos.close();
                dis.close();
            } catch(UnknownHostException ex1){
                ex1.printStackTrace();
            } catch(IOException ex2) {
                ex2.printStackTrace();
            }
        }
    }

    public class Message {
        public String id;

        public Message(String id){
            this.id = id;
        }
    }

    public class UserMessage extends Message {
        public String userid;
        public String username;

        public UserMessage(String id, String userid, String username){
            super(id);
            this.userid = userid;
            this.username = username;
        }
    }

    public class CreateUser extends UserMessage {
        public String enteredUserName;
        public byte[] enteredPassword;

        public CreateUser(String id, String userid, String username, String enteredUserName, byte[] password) {
            super(id, userid, username);
            this.userid = userid;
            this.username = username;
            this.enteredUserName = enteredUserName;
            this.enteredPassword = password;
        }
    }
}
