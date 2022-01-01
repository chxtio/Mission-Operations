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

var messageCount = 0;
var alertCount = 0;
var lvDict = { "Bird-9": 1, "Bird-Heavy": 2, "Hawk-Heavy": 3 }
var pDict = { "GPM": 1, "TDRS-11": 2, "RO-245": 3 }
var lVehicles = { 1: { name: "Bird-9", launchStatus: document.getElementById("status1").innerText, deployStatus: document.getElementById("deploy1").innerText, payload: "GPM" }, 2: { name: "Bird-Heavy", launchStatus: document.getElementById("status2").innerText, deployStatus: document.getElementById("deploy2").innerText, payload: "TDRS-11" }, 3: { name: "Hawk-Heavy", launchStatus: document.getElementById("status3").innerText, deployStatus: document.getElementById("deploy3").innerText, payload: "RO-245" } };
$('#alertBtn').hide();
$('#payloadBtn').hide();

// Subscribe to onMessageReceived (client-side hub method) to show incoming messages from backend
signalrConnection.on("onMessageReceived", function (eventMessage) {
    //messageCount++;
    //const msgCountH4 = document.getElementById("messageCount");
    //msgCountH4.innerText = "Total Telemetry Packets: " + messageCount.toString();

    //console.log("Messages: " + messageCount.toString());

    for (const property in eventMessage) {

        //console.log("property: " + property + " value: " + eventMessage[property]);

        if (property === "title") {
            console.log(eventMessage["title"]);
            var json = JSON.parse(eventMessage["title"]);
            var type = json["Type"];
            var id = json["LvId"];
            var createdDateTime = json["CreatedDateTime"];
            var p = "";
            console.log("type: " + type);

            if (type === "launch_command") {
                if (json["Status"] === "Launched") {
                    launch(id, createdDateTime);
                }                
            } else if (type === "Reached Orbit Alert") {
                console.log("Reached Orbit Alert");
                reachedOrbit(id, createdDateTime);
            }
            else {
                if (type == "payload_command") {
                    p = "p";
                }

                var count = json["Count"];
                var altitude = json["Altitude"];
                var longitude = json["Longitude"];
                var latitude = json["Latitude"];
                var temperature = json["Temperature"];
                var timeToOrbit = json["TimeToOrbit"];                

                //document.getElementById("exampleModalLabel" + p + id).innerText = 
                document.getElementById("tlmCount" + p + id).innerText = "Telemetry Received: " + count.toString();
                document.getElementById("altitude" + p + id).innerText = altitude;
                document.getElementById("longitude" + p + id).innerText = longitude;
                document.getElementById("latitude" + p + id).innerText = latitude;
                document.getElementById("temperature" + p + id).innerText = temperature;
                if (type !== "payload_command") {
                    if (timeToOrbit < 0) {
                        timeToOrbit = 0;
                    }
                    document.getElementById("time_to_orbit" + id).innerText = timeToOrbit;
                    document.getElementById("tto" + id).innerText = timeToOrbit + " second(s)";
                }
                document.getElementById("time_formatted" + p + id).innerText = createdDateTime;

/*                console.log("id: " + p + id);*/
            }          

        }    

    }

});


// Catch commands on client side
$(document).ready(function () {
    // -------------------Launch Form ---------------------------------------------------
    $('#launch_target_select').change(function () {
        const target = $(this).find(":selected").text();
        console.log("launch form | Target: " + target);

        var lvId = lvDict[target];

        var launchStatus = lVehicles[lvId]["launchStatus"];
        console.log("launch status: " + launchStatus);

        if (launchStatus === "Launched" || target === "Select") {
            console.log("launch form | disable " + target);
            $('#launch_cmd_btn').prop('disabled', true);            
            $('#launch_target' + lvId).prop('disabled', true);
        } else {
            $('#launch_target' + lvId).prop('disabled', false);
            $('#launch_cmd_btn').prop('disabled', false);
        }

    })

    // Sends launch command info
    $('#launch_cmd_form').submit(function (e) {
        e.preventDefault();
        const target = $('#launch_target_select').find(":selected").text();

        signalrConnection.invoke("cmdReceived", "launch_command", target, "Launch").catch(function (err) {
            return console.error(err.toString());
        });

        var lvId = lvDict[target];
        $('#launch_target_select').val('0');
        $('#launch_target0').prop('disabled', true);
        $('#launch_target' + lvId).prop('disabled', true);
        $('#launch_cmd_btn').prop('disabled', true);
    })

    // -------------------Command Center -------------------------------------------------
    $('#targetSelect').change(function () {
        const target = $(this).find(":selected").text();
        const cmd = $('#commandSelect').find(":selected").text();
        //console.log("Target: " + target + " Command: " + cmd);

        //console.log(lVehicles);
        var lvId = lvDict[target];
        var launchStatus = lVehicles[lvId]["launchStatus"]
        var deployStatus = lVehicles[lvId]["deployStatus"];        
        console.log("launchStatus: " + launchStatus);
        console.log(target + "payload deployStatus: " + deployStatus);

        if (launchStatus === "Launched") {
            $('#deorbitBtn').prop('disabled', false);
            $('#startTlmBtn').prop('disabled', false);
            $('#stopTlmBtn').prop('disabled', false);
            if (deployStatus === "Ready to Deploy") {
                console.log("Unlock deploy btn");
                $('#deployBtn').prop('disabled', false);
            } else {
                $('#deployBtn').prop('disabled', true);
            }
        } else if (launchStatus === "Upcoming") {
            $('#deorbitBtn').prop('disabled', true);
            $('#startTlmBtn').prop('disabled', true);
            $('#stopTlmBtn').prop('disabled', true);
            $('#deployBtn').prop('disabled', true);
        }

        if (cmd === "Select") {
            $('#cmd_btn').prop('disabled', true);
        } else
            $('#cmd_btn').prop('disabled', false);
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

    // Sends command and target info
    $('#cmd_form').submit(function (e) {
        e.preventDefault();
        const target = $('#targetSelect').find(":selected").text();
        const cmd = $('#commandSelect').find(":selected").text();
        var text = $('#cmdHistory')
        text.val(text.val() + target + " | " + cmd + "\n");
        console.log("Target: " + target + " Command: " + cmd);

        signalrConnection.invoke("cmdReceived", "command", target, cmd).catch(function (err) {
            return console.error(err.toString());
        });

        if (cmd === "DeployPayload") {
            deploy(lvDict[target]);
        }

        $('#targetSelect').val('0');
        $('#commandSelect').val('0');
    })

    // -------------------Payload Command Center -------------------------------------------------
    $('#payload_command_select').change(function () {
        const cmd = $(this).find(":selected").text();
        const target = $('#payload_target_select').find(":selected").text();
        //console.log("Target: " + target + " Command: " + cmd);

        if (target === "Select") {
            $('#payload_cmd_btn').prop('disabled', true);
        } else
            $('#payload_cmd_btn').prop('disabled', false);
    })

    // Sends payload command info
    $('#payload_cmd_form').submit(function (e) {
        e.preventDefault();
        const target = $('#payload_target_select').find(":selected").text();
        const cmd = $('#payload_command_select').find(":selected").text();
        var text = $('#payload_cmd_history');
        text.val(text.val() + target + " | " + cmd + "\n");
        console.log("Target: " + target + " Command: " + cmd);

        signalrConnection.invoke("cmdReceived", "payload_command", target, cmd).catch(function (err) {
            return console.error(err.toString());
        });

        $('#payload_target_select').val('0');
        $('#payload_command_select').val('0');
    })

    $('.modal').on('show.bs.modal', function (e) {
        if (e.currentTarget.id === "alertModal") {
            alertCount = 0;
            $('#alertBtn').hide();
        }
    })
});

function reachedOrbit(lvId, createdTime) {
    // Change payload status to ready to deploy
    var deployStatus = document.getElementById("deploy" + lvId);
    if (deployStatus.innerHTML === "Ready to Deploy" || deployStatus.innerHTML === "Deployed") {
        return;
    }
    deployStatus.innerHTML = "Ready to Deploy";
    deployStatus.className = "deploy-status open";
    lVehicles[lvId]["deployStatus"] = "Ready to Deploy";

    var alertMessage = createdTime + "  |  Ready to deploy " + lVehicles[lvId]["payload"];
    alert(alertMessage);
}

function deploy(lvId) {
    // Change payload status to deployed
    var deployStatus = document.getElementById("deploy" + lvId);
    deployStatus.innerHTML = "Deployed";
    deployStatus.className = "deploy-status open";
    lVehicles[lvId]["deployStatus"] = "Deployed";
    $('#choosePayl' + lvId).prop('disabled', false);
    $('#payloadBtn').show();
}

function launch(lvId, createdTime) {
    console.log("launch(): ");
    lVehicles[lvId]["launchStatus"] = "Launched";
    var launchStatus = document.getElementById("status" + lvId);
    launchStatus.innerHTML = "Launched";
    launchStatus.className = "success";
    console.log("test launch status: " + lVehicles[lvId]["launchStatus"]);

    var alertMessage = createdTime + "  |  " + lVehicles[lvId]["name"] + " has launched";
    alert(alertMessage);

    var deployStatus = document.getElementById("deploy" + lvId);
    deployStatus.innerHTML = "In Progress";
    deployStatus.className = "deploy-status in-progress";
    lVehicles[lvId]["deployStatus"] = "In Progress";
}

function alert(message) {
    $('#alertBtn').show();
    alertCount++;
    document.getElementById("alertBadge").innerHTML = alertCount;
    var text = $('#alert_history');
    text.val(text.val() + message + "\n");
}