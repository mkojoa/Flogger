const FloggerUsageLog = (message, additionalInfo) => {
    const logEntry = getJsLoggingInfo(message, additionalInfo);

    $.ajax({
        method: "POST",
        url: `${window.location.href}api/v1/flogger-core/usages`,
        contentType: "application/json",
        data: JSON.stringify(logEntry)
    })
}, FloggerDiagnosticLog = (message, additionalInfo) => {
    const logEntry = getJsLoggingInfo(message, additionalInfo);

    $.ajax({
        method: "POST",
        url: `${window.location.href}api/v1/flogger-core/diagnostic`,
        contentType: "application/json",
        data: JSON.stringify(logEntry)
    })
}, FloggerErrorLog = (message, res, additionalInfo) => {
    const logEntry = getJsLoggingInfo(message, additionalInfo);
    logEntry.CorrelationId = res.replace(/ .*ErrorId: /gi, "");  //res.replace(/ .*error id: /gi, "");
    $.ajax({
        method: "POST",
        url: `${window.location.href}api/v1/flogger-core/errors`,
        contentType: "application/json",
        data: JSON.stringify(logEntry)
    })
}, FloggerPerformanceLogStart = (message, additionalInfo) => {
    const sessionKey = btoa(message);
    sessionStorage[sessionKey] = JSON.stringify(getJsLoggingInfo(message, additionalInfo))
}, FloggerPerformanceLogStop = (message) => {
    const sessionKey = btoa(message);
    const now = new Date();
    const logEntry = JSON.parse(sessionStorage[sessionKey]);
    logEntry.ElapsedMilliseconds = now - Date.parse(logEntry.Timestamp);

    $.ajax({
        method: "POST",
        url: `${window.location.href}api/v1/flogger-core/performance`,
        contentType: "application/json",
        data: JSON.stringify(logEntry)
    })
}, getJsLoggingInfo = (message, additionalInfo) => {
    const logInfo = {};
    logInfo.Timestamp = new Date();
    logInfo.Location = window.location.toString();
    logInfo.product = "Payroll Service API";
    logInfo.Layer = "Payroll Client JS";
    logInfo.Message = message;
    logInfo.Hostname = window.location.hostname;
    
    //add param
    logInfo.UserId = "";
    logInfo.UserName = "";
    
    logInfo.AdditionalInfo = {};
    if (additionalInfo){
        for (const property in additionalInfo){
            if (additionalInfo.hasOwnProperty(property)){
                logInfo.AdditionalInfo[property] = additionalInfo[property]
            }
        }
    }
    
    return logInfo;
};