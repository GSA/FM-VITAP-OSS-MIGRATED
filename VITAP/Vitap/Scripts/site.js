/*

Site-wide library of javascript functions, modules etc.

*/

// Extension method added to JavaScript string to do the url encoding.

String.prototype.encodeUriComponent = function () {    
    return encodeURIComponent(this);
}

String.prototype.StartsWith = function (snippet) {
    return (this.lastIndexOf(snippet, 0) === 0);
}

function ConvertNullToEmptyString(string) {
    if (string == null) {
        return '';
    }
    return string;
}

var gvt_Warning =
    '                                         WARNING!                           \n' +
    'This is a U.S. General Services Administration Federal Government computer system that is "FOR OFFICIAL USE ONLY." ' +
    'This system is subject to monitoring. Therefore, no expectation of privacy is to be assumed. ' +
    'Individuals found performing unauthorized activities are subject to disciplinary action including criminal prosecution.';

function showSessionTimeoutWarning() {
    var now = new Date();
    var hours = now.getHours();
    var minutes = now.getMinutes();
    var seconds = now.getSeconds();
    var timeValue = "" + ((hours > 12) ? hours - 12 : hours)
    if (timeValue == "0") timeValue = 12;
    timeValue += ((minutes < 10) ? ":0" : ":") + minutes
    timeValue += ((seconds < 10) ? ":0" : ":") + seconds
    timeValue += (hours >= 12) ? " P.M." : " A.M.";
    alert('WARNING: Your session idle time has reached 25 minutes. \r Time of Warning (US Central Time) - ' +
        timeValue + '\r VITAP will time out 5 minutes after the warning time.');
}

function SessionEnd(url) {
    location = url;
}

function CreateDialogWithURL(dialogSelector, newURL) {
    var popup = $(dialogSelector);
    if (popup.data("kendoWindow")) {
        popup.data("kendoWindow").open();
    }
    else {
        popup.kendoWindow();
    }
    var dialog = $(dialogSelector).data("kendoWindow");
    dialog.setOptions({
        width: 800,
        height: 800
    });

    dialog.refresh({
        url: newURL
    });
}

function ViewTiffImage(dialogSelector, controller, action, tiffType, tiffTypeId) {
    var location = window.location;
    var newURL = location.protocol + "//" + location.host + "/" + controller + "/" + action
        + "?Type=" + tiffType + "&TypeId=" + encodeURIComponent(tiffTypeId);
    CreateDialogWithURL(dialogSelector, newURL);
}

function ViewTiffImageWithUrl(dialogSelector, url, tiffType, tiffTypeId) {
    var location = window.location;
    var newURL = url + "?Type=" + tiffType + "&TypeId=" + encodeURIComponent(tiffTypeId);
    CreateDialogWithURL(dialogSelector, newURL);
}

function ViewTiffImageWithPath(dialogSelector, controller, action, tiffType, tiffTypeId, tiffFilePath) {
    var location = window.location;
    var newURL = location.protocol + "//" + location.host + "/" + controller + "/" + action
        + "?Type=" + tiffType + "&TypeId=" + encodeURIComponent(tiffTypeId) + "&FilePath=" + encodeURIComponent(tiffFilePath);
    CreateDialogWithURL(dialogSelector, newURL);
}

function ConfigureKendoGridPaginationControlsFor508(gridId) {
    var grid = $('#' + gridId).data('kendoGrid');
    if (grid == null)
        return;

    ConfigureFirstPageControl(gridId);
    ConfigureLastPageControl(gridId);
    ConfigurePrevPageControl(gridId);
    ConfigureNextPageControl(gridId);
    ConfigurePageSize(gridId);
}

function ConfigureFirstPageControl(gridId) {
    $("#" + gridId + " .k-grid-pager .k-pager-nav span.k-i-arrow-end-left")
        .removeClass("k-icon")
        .text("First Page")
        .parent().css({
            'padding': '0 6px',
            'border-radius': '4px 0 0 4px',
            'text-decoration': 'none'
        });
}

function ConfigureLastPageControl(gridId) {
    $("#" + gridId + " .k-grid-pager .k-pager-nav span.k-i-arrow-end-right")
        .removeClass("k-icon")
        .text("Last Page")
        .parent().css({
            'padding': '0 6px',
            'border-radius': '4px 0 0 4px',
            'text-decoration': 'none'
        });
}

function ConfigurePrevPageControl(gridId) {
    $("#" + gridId + " .k-grid-pager .k-pager-nav span.k-i-arrow-60-left")
        .removeClass("k-icon")
        .text("Prev Page")
        .parent().css({
            'padding': '0 6px',
            'border-radius': '4px 0 0 4px',
            'text-decoration': 'none'
        });
}

function ConfigureNextPageControl(gridId) {
    $("#" + gridId + " .k-grid-pager .k-pager-nav span.k-i-arrow-60-right")
        .removeClass("k-icon")
        .text("Next Page")
        .parent().css({
            'padding': '0 6px',
            'border-radius': '4px 0 0 4px',
            'text-decoration': 'none'
        });
}

function ConfigurePageSize(gridId) {
    $("#" + gridId + " .k-pager-sizes select").attr("id", "pageSize");
    $("#" + gridId + " .k-pager-sizes").before('<label for="pageSize" style="padding-left: 30px; margin-right: -20px;">Page Size</label>');
}

function MakeKendoGridScrollable(gridId, gridHeight) {
    var grid = $('#' + gridId).data('kendoGrid');
    if (grid == null)
        return;

    var wrapperId = gridId + "VitapGridWrapper";
    $("#" + gridId + " table").wrap("<div id='" + wrapperId + "'></div>");
    $("#" + wrapperId).css({
        "height": gridHeight,
        "overflow-y": "scroll"
    });
}

var kendoAlert = (function () {
    var dfrd;
    // Must wait until the kendo scripts have loaded.
    if (typeof kendo === "undefined") return;
    var html = kendo.template("<div style='min-height: 80px'><span class='k-icon k-warning'></span><span style='padding-left: 5px;'>#= data #</span></div><div class='k-block' style='text-align: right'><button id='closeAlert' class='k-button' style='width: 60px;'>Ok</button></div>");

    var win = $("<div id='kendoAlert'>").kendoWindow({
        modal: true,
        visible: false,
        deactivate: function () {
            dfrd.resolve();
        },
        animation: {
            open: {
                effects: "slideIn:down fadeIn"
            },
            close: {
                effects: "slideIn:down fadeIn",
                reverse: true
            }
        },
        width: 300,
        position: {
            top: 10,
            left: ($(window).width() / 2) - 150
        }
    }).getKendoWindow();

    var open = function (msg, title) {
        dfrd = $.Deferred();

        win.content(html(msg));
        win.title(title);

        win.center().open();

        return dfrd;
    };

    win.wrapper.on("click", "#closeAlert", function () {
        win.close();
    });

    return open;
}());

var kendoYesNoDialog = (function () {
    var dfrd;
    var result = false;

    // Must wait until the kendo scripts have loaded.
    if (typeof kendo === "undefined") return;
    var html = kendo.template("<div style='min-height: 80px'><span style='padding-left: 5px;'>#= data #</span></div><div class='k-block' style='text-align: right'><button id='yesButton' class='k-button' style='width: 60px;'>Yes</button><button id='noButton' class='k-button' style='width: 60px;'>No</button></div>");

    var win = $("<div id='kendoYesNoDialog'>").kendoWindow({
        modal: true,
        visible: false,
        deactivate: function () {
            dfrd.resolve(result);
        },
        animation: {
            open: {
                effects: "slideIn:down fadeIn"
            },
            close: {
                effects: "slideIn:down fadeIn",
                reverse: true
            }
        },
        width: 300,
        position: {
            top: 10,
            left: ($(window).width() / 2) - 150
        }
    }).getKendoWindow();

    var open = function (msg, title, callback) {
        dfrd = $.Deferred();
        dfrd.done(callback);

        win.content(html(msg));
        win.title(title);

        win.center().open();

        return dfrd;
    };

    win.wrapper.on("click", "#yesButton", function () {
        result = true;
        win.close();
    });

    win.wrapper.on("click", "#noButton", function () {
        win.close();
    });

    return open;
}());