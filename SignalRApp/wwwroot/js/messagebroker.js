"use strict"

var lvDict = { "Bird-9": 1, "Bird-Heavy": 2, "Hawk-Heavy": 3 }

// Generate client-side SignalR Hub proxy
const signalrConnection = new signalR.HubConnectionBuilder()
    .withUrl("/messagebroker")
    .configureLogging(signalR.LogLevel.Information)
    .build();

// Invoke connect method and log any errors
signalrConnection.start().then(function () {
    console.log("SignalR Hub Connected!");
}).catch(function (err) {
    return console.error(err.toString());
});

let messageCount = 0;

// Subscribe to onMessageReceived (client-side hub method) to show incoming messages from backend
signalrConnection.on("onMessageReceived", function (eventMessage) {
    messageCount++;
    const msgCountH4 = document.getElementById("messageCount");
    msgCountH4.innerText = "Telemetry Packets: " + messageCount.toString();
    //const ul = document.getElementById("messages");
    //const li = document.createElement("li");
    //li.innerText = "#" + messageCount.toString();

    console.log("Messages: " + messageCount.toString());
    //console.log(JSON.stringify(eventMessage));

    for (const property in eventMessage) {

        console.log("property: " + property + " value: " + eventMessage[property]);

        if (property === "title") {
            console.log(eventMessage["title"]);
            var json = JSON.parse(eventMessage["title"]);
            var lvId = json["LvId"];
            var altitude = json["Altitude"];
            var longitude = json["Longitude"];
            var latitude = json["Latitude"];
            var temperature = json["Temperature"];
            var timeToOrbit = json["TimeToOrbit"];
            var createdDateTime = json["CreatedDateTime"];

            document.getElementById("altitude" + lvId).innerText = altitude;
            document.getElementById("longitude" + lvId).innerText = longitude;
            document.getElementById("latitude" + lvId).innerText = latitude;
            document.getElementById("temperature" + lvId).innerText = temperature;
            document.getElementById("time_to_orbit" + lvId).innerText = timeToOrbit;
            document.getElementById("tto" + lvId).innerText = timeToOrbit;
            document.getElementById("time_formatted" + lvId).innerText = createdDateTime;

            var lvId = lvDict[localStorage.target];
            console.log("lvId: " + lvId);
            console.log("tto: " + timeToOrbit);

            if (timeToOrbit === 0) {
                console.log("Change payload status to ready to deploy");
                var deployStatus = document.getElementById("deploy" + lvId);
                deployStatus.innerHTML = "Ready to Deploy";
                deployStatus.className = "deploy-status open";
            }

            //console.log("timestamp: " + createdDateTime + "altitude: " + altitude + " \nlongitude: " + longitude + "\nlatitude: " + latitude + "\ntemperature: " + temperature + "\ntimeToOrbit: " + timeToOrbit);

            //for (const key in json) {
            //    const newDiv = document.createElement("div");
            //    const classAttrib = document.createAttribute("style");
            //    classAttrib.value = "font-size: 80%;";
            //    newDiv.setAttributeNode(classAttrib);
            //    const newContent = document.createTextNode(`${key}: ${json[key]}`);
            //    newDiv.appendChild(newContent);
            //    li.appendChild(newDiv);
            //}
        }    

    }

    //ul.prepend(li);
});



// Catch commands on client side
$(document).ready(function () {

    $('[name="chkColor"]').change(function () {
        const state = $(this).prop('checked');
        const lightColor = $(this).attr('data-lightColor');

        console.log("state: " + state);
        console.log("lightColor: " + lightColor);

        //signalrConnection.invoke("CommandReceived", lightColor, state).catch(function (err) {
        //    return console.error(err.toString());
        //});
        event.preventDefault();
    })

    $('#targetSelect').change(function () {
        const target = $(this).find(":selected").text();
        const cmd = $('#commandSelect').find(":selected").text();
        //console.log("Target: " + target + " Command: " + cmd);

        //var status1 = localStorage.stat1;
        //var status2 = localStorage.stat2;
        //var status3 = localStorage.stat3;
        //console.log("status1: " + status1 + "\nstatus2: " + status2 + "\nstatus3: " + status3);
        var statusDict = { 1: localStorage.stat1, 2: localStorage.stat2, 3: localStorage.stat3 };
        var lvId = lvDict[target];
        var status = statusDict[lvId];
        console.log("status" + lvId + ": " + status);

        if (status === "Launched") {
            $('#deorbitBtn').prop('disabled', false);
            $('#startTlmBtn').prop('disabled', false);
            $('#stopTlmBtn').prop('disabled', false);
        } else if (status === "Upcoming") {
            $('#deorbitBtn').prop('disabled', true);
            $('#startTlmBtn').prop('disabled', true);
            $('#stopTlmBtn').prop('disabled', true);
        }

        if (cmd === "Select") {
            $('#cmd_btn').prop('disabled', true);
        } else
            $('#cmd_btn').prop('disabled', false);

        localStorage.target = target;
    })

    $('#commandSelect').change(function () {
        const cmd = $(this).find(":selected").text();
        const target = $('#targetSelect').find(":selected").text();
        //console.log("Target: " + target + " Command: " + cmd);

        if (target === "Select") {
            $('#cmd_btn').prop('disabled', true);
        } else
            $('#cmd_btn').prop('disabled', false);
    })

    $('#cmd_form').submit(function (e) {
        e.preventDefault();
        const target = $('#targetSelect').find(":selected").text();
        const cmd = $('#commandSelect').find(":selected").text();
          var text = $('#cmdHistory')
        text.val(text.val() + cmd + "\n");
        console.log("Target: " + target + " Command: " + cmd);

        signalrConnection.invoke("cmdReceived", target, cmd).catch(function (err) {
            return console.error(err.toString());
        });

    })

});


