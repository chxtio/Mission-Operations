////"use strict";

////// Client reference to signalR hub in order to connect and receive messages
////const signalRConnection = new signalR.HubConnectionBuilder()
////    .withUrl("/messagebroker")
////    .configureLogging(signalR.LogLevel.Information)
////    .build();

////// Invoke connect method and log any errors
////signalRConnection.start().then(function () {
////    console.log("SignalR Hub Connected!");
////}).catch(function (err) {
////    return console.error(err.toString());
////});

////let msgCount = 0;

////// Subscribe to onMessageReceived method to show incoming messages from backend
////signalRConnection.on("onMessagedReceived", function (eventMessage) {
////    msgCount++;
////    const msgCountH4 = document.getElementById("messageCount");
////    msgCountH4.innerText = "Messages: " + msgCount.toString();
////    const ul = document.getElementById("messages");
////    const li = document.createElement("li");
////    li.innerText = msgCount.toString();

////    for (const prop in eventMessage) {
////        const newDiv = document.createElement("div");
////        const classAttrib = document.createAttribute("style");
////        classAttrib.value = "font-size: 80%";
////        newDiv.setAttributeNode(classAttrib);
////        const newContent = document.createTextNode(`${prop}: ${eventMessage[prop]}`);
////        newDiv.appendChild(newContent);
////        li.appendChild(newDiv);
////    }

////    ul.prependChild(li);
////    //window.scrollTo(0, document.body.scrollHeight);
////    //console.log(eventMessage);
////}); 

////// Catch command on client side
////$(document).ready(function () {

////    $('[name="chkColor"]').change(function () {
////        const state = $(this).prop('checked');
////        const lightColor = $(this).attr('data-lightColor');

////        signalRConnection.invoke("CommandReceived", lightColor, state).catch(function (err) {
////            return console.error(err.toString());
////        });
////        event.preventDefault();
////    })
////});



"use strict"

const signalrConnection = new signalR.HubConnectionBuilder()
    .withUrl("/messagebroker")
    .configureLogging(signalR.LogLevel.Information)
    .build();

signalrConnection.start().then(function () {
    console.log("SignalR Hub Connected");
}).catch(function (err) {
    return console.error(err.toString());
});

let messageCount = 0;

signalrConnection.on("onMessageReceived", function (eventMessage) {
    messageCount++;
    const msgCountH4 = document.getElementById("messageCount");
    msgCountH4.innerText = "Messages: " + messageCount.toString();
    const ul = document.getElementById("messages");
    const li = document.createElement("li");
    li.innerText = messageCount.toString();

    for (const property in eventMessage) {
        const newDiv = document.createElement("div");
        const classAttrib = document.createAttribute("style");
        classAttrib.value = "font-size: 80%;";
        newDiv.setAttributeNode(classAttrib);
        const newContent = document.createTextNode(`${property}: ${eventMessage[property]}`);
        newDiv.appendChild(newContent);
        li.appendChild(newDiv);
    }

    ul.prepend(li);
});

$(document).ready(function () {

    $('[name="chkColor"]').change(function () {
        const state = $(this).prop('checked');
        const lightColor = $(this).attr('data-lightColor');

        signalrConnection.invoke("CommandReceived", lightColor, state).catch(function (err) {
            return console.error(err.toString());
        });
        event.preventDefault();
    })
});