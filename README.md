# **BroadcastServer**

## **Description**

Simple WebSocket CLI application that allows you to run small WebSocket server and use it to broadcast messages to all connected clients.

Broadcast-Server wast task to build simple CLI tool from roadmap.sh backend intermediate projects. 
https://roadmap.sh/projects/broadcast-server


__________________________________________________________________________________________________________________________


## **Features **

- Ability to run multiple WebSocket clients and servers on different ports
- Clients can send messages to the server, which will broadcast them to all connected clients.


__________________________________________________________________________________________________________________________


## **Technologies used**

- Language: C#
- Framework: .NET 8

____________________________________________________________________________________________________________________________


## Run the app

```bash
git clone https://github.com/karppinski/BroadcastServer/.git


```
## If you want to use a specific port
```
cd BroadcastServer/BroadcastServer

dotnet run --port # You can use ports between 7000-7099

cd BroadcastServer/BroadcastClient

dotnet run --port # You can use ports between 7000-7099

```
## If You want to use default port
```
cd BroadcastServer/BroadcastServer

dotnet run 

cd BroadcastServer/BroadcastClient

dotnet run 

```

## Or there is a simpler option. Project folder contains AppStarter.bat

##### Run AppStarter.bat exectulable and one instance of server and client will fire up on default port.

____________________________________________________________________________________________________________________________________

# **Future plans**

- I don't think its functionalities will be upgraded somehow. I wanted to build this app to understand basics of Websocket connections and it is done.

- One thing I plan to add is basic integration tests, as I have never implemented them and want to try it out.
  
### **BUT**

### I am aware of things like
- Client retry mechanism can be improved, I am aware that there are libraries for this, but I wanted to write it easiest way possible.
- I am shure that checking port mechanism is not efficient and can be written better.
- There should be some logging but I skipped it.
- And some other improvements that could be done, but it wasn't main concern when i started this app.

_____________________________________________________________________________________________________________________________________________

# **Task source**

https://roadmap.sh/projects/broadcast-server
