# Chat Application





## Summary

* This project demonstrate the case where a user can register, login, choose a chat room and start chatting with others in the room.
* WebSocket is used for asynchronously sending messages to the people in the same room.



## Technology

* ASP.NET Core
* WebSocket
* MongoDB



## Installation

* You need to install MongoDB and create a db. Change the DB settings in the projects.



## Usage

* Use `dotnet run` command to start the server.
* Open up a chrome window and open up another chrome window in incognito mode.
* In the index page, you will see a link that takes you to ChatRoom. You will be redirected to login screen if you are not logged in yet. If you need to sign up first, first sign up and then login. Do this for both chrome windows.
* Choose one of the chatrooms. If you don't see a chatroom, you will need to create one.
* Both users should enter the same room.
* If you type a message and click send, you will see the message in both sides.