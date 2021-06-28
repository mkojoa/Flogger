// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$("#justAClickBtn").on( "click",function()
    {
        const addInfo = {
           "customValue": "some extra value here"
        };
        
        FloggerUsageLog(`Just a button click`, addInfo);
    }
);

$("#ApiCallBtn").on( "click",function()
    {
        FloggerPerformanceLogStart("Api Call Started")
        $.ajax({
            method: "GET",
            url: "https://localhost:5001/api/v1/flogger-core/get-api-messages",
        }).done(function (data){
            console.log(data);
        });
        FloggerPerformanceLogStop("Api Call Started")
    }
);

$("#FailedApiCallBtn").on( "click",function()
    {
        FloggerDiagnosticLog("Started Update Salary Grade");

        const apiUpdateRecord = {
            "Id": 1,
            "UserName": "Michael Ameyaw",
            "Age": 1000,
            "Phone": "0000000000",
            "Location": "Somewhere"
        };
        $.ajax({
            method: 'PUT',
            url: 'https://localhost:5001/api/v1/flogger-core/get-api-messages-update',
            contentType : "application/json",
            data : JSON.stringify(apiUpdateRecord)
        }).done(function (data){
            console.log(data);
        }).fail(function (msg){
           FloggerErrorLog('Failed Retrieving api data', msg.responseText)
        });
        FloggerDiagnosticLog("End Update Salary Grade");
    }
);


