"use strict"
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
    msgCountH4.innerText = "Telemetry packets: " + messageCount.toString();
    const ul = document.getElementById("messages");
    const li = document.createElement("li");
    li.innerText = "#" + messageCount.toString();

    console.log("Messages: " + messageCount.toString());
    //console.log(JSON.stringify(eventMessage));



    for (const property in eventMessage) {
        console.log("property: " + property + " value: " + eventMessage[property]);
        if (property === "title") {
            var json = JSON.parse(eventMessage["title"]);
            var altitude = json["altitude"];
            var longitude = json["longitude"];
            var latitude = json["latitude"];
            var temperature = json["temperature"];
            var timeToOrbit = json["timeToOrbit"];
            var createdDateTime = json["createdDateTime"];

            console.log("altitude: " + altitude + " \nlongitude: " + longitude + "\nlatitude: " + latitude + "\ntemperature: " + temperature + "\ntimeToOrbit: " + timeToOrbit + "\ncreatedDateTime: " + createdDateTime);
            for (const key in json) {
                const newDiv = document.createElement("div");
                const classAttrib = document.createAttribute("style");
                classAttrib.value = "font-size: 80%;";
                newDiv.setAttributeNode(classAttrib);
                const newContent = document.createTextNode(`${key}: ${json[key]}`);
                newDiv.appendChild(newContent);
                li.appendChild(newDiv);
            }
        }
        //else {
        //    const newDiv = document.createElement("div");
        //    const classAttrib = document.createAttribute("style");
        //    classAttrib.value = "font-size: 80%;";
        //    newDiv.setAttributeNode(classAttrib);
        //    const newContent = document.createTextNode(`${property}: ${eventMessage[property]}`);
        //    newDiv.appendChild(newContent);
        //    li.appendChild(newDiv);

        //}
            

    }

    ul.prepend(li);
});

// Catch commands on client side
$(document).ready(function () {

    $('[name="chkColor"]').change(function () {
        const state = $(this).prop('checked');
        const lightColor = $(this).attr('data-lightColor');

        console.log("state: " + state);
        console.log("lightColor: " + lightColor);

        signalrConnection.invoke("CommandReceived", lightColor, state).catch(function (err) {
            return console.error(err.toString());
        });
        event.preventDefault();
    })

    $('#cmd_form').submit(function (e) {
        e.preventDefault();
        const target = $('#targetSelect').find(":selected").text();
        console.log("Target: " + target);

        //const cmd = $(this).find(":selected").text();
        const cmd = $('#commandSelect').find(":selected").text();
        console.log("Command: " + cmd);
          var text = $('#cmdHistory')
        text.val(text.val() + cmd + "\n");

        signalrConnection.invoke("cmdReceived", target, cmd).catch(function (err) {
            return console.error(err.toString());
        });

    })

        //$('#targetSelect').change(function () {
    //    const target = $(this).find(":selected").text();
    //    //console.log("Target: " + target);
    //})

    //$('#commandSelect').change(function () {
    //    const target = $('#targetSelect').find(":selected").text();
    //    console.log("Target: " + target);

    //    const cmd = $(this).find(":selected").text();
    //    console.log("Command: " + cmd);
    //      var text = $('#cmdHistory')
    //    text.val(text.val() + cmd + "\n");

    //    signalrConnection.invoke("cmdReceived", target, cmd).catch(function (err) {
    //        return console.error(err.toString());
    //    });
    //    event.preventDefault();
    //})

    //$("select")
    //    .change(function () {
    //        var str = "";
    //        $("select option:selected").each(function () {
    //            str += $(this).text() + " ";
    //        });
    //        $("div").text(str);
    //    })
    //    .trigger("change");





});


