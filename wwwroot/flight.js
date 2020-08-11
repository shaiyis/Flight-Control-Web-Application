
// variables
const markers = new Array();
const flightPath = { flightId: null, polyLine: null };
const flightsIdsSet = new Set();

function initFlightPath() {
    flightPath.flightId = null;
    flightPath.polyLine = null;
}

// configurations:
// add google map a listener for click():
map.addListener('click', function () {
    // when clicking on map, remove all colored paths or table marks

    $("#internalFlights tr").removeClass('table-success');
    $("#externalFlightsBody tr").removeClass('table-success');
    $("#flightDetails > tbody").html("");
    if (flightPath.polyLine !== null) {
        flightPath.polyLine.setMap(null);
        initFlightPath();
    }
});


function removeMarkers() {
    markers.forEach(function (marker) {
        marker.setMap(null);
    });
    while (markers.length > 0) {
        markers.pop();
    }
}

function showPlaneIcon(lat, lon, flightID) {

    let position = new google.maps.LatLng(lat, lon);
    let marker = new google.maps.Marker({
        map: map,
        position: position,
        icon: '/Images/plane.png'
    });

    marker.addListener('click', function () {
        map.setCenter(marker.getPosition());
        smoothZoom(map, 8, map.getZoom());
        showFlight(flightID);

    })
    markers.push(marker);
}

function movePlanes() {
    const currentTime = new Date().toISOString().substr(0, 19);
    const timeFormat = currentTime + 'Z';
    const ask = "/api/Flights?relative_to=" + timeFormat + "&sync_all";
    $.getJSON(ask, function (data) {
        removeMarkers();
        data.forEach(function (flight) {
            showPlaneIcon(flight.latitude, flight.longitude, flight.flight_id);
        })
    })
        .fail(function (jqXHR) {
            toastr.error(jqXHR.statusText + ' : ' + jqXHR.responseText);
        })
        ;
}

function removeInactiveFlights() {
    //check internal flights table
    $('#internalFlights tr').each(function () {

        let id = this.id;
        let inSet = flightsIdsSet.has(id);
        let inDetails = (document.getElementById("details_" + id) !== null);
        let inInternalOrExternal = (document.getElementById(id) !== null);
        let pathPaintedOnScreen = (flightPath.flightId === id && flightPath.polyLine !== null);

        // remove polyline
        if (!inSet && pathPaintedOnScreen) {
            flightPath.polyLine.setMap(null);
            initFlightPath();
        }

        // remove from details
        if (!inSet && inDetails) {
            document.getElementById("details_" + id).remove();
        }

        //THIRD: remove from table
        if (!inSet && inInternalOrExternal) {
            document.getElementById(id).remove();
        }
    });

    $('#externalFlights tr').each(function () {

        let id = this.id;
        let inSet = flightsIdsSet.has(id);
        let inDetails = (document.getElementById("details_" + id) !== null);
        let inInternalOrExternal = (document.getElementById(id) !== null);
        let pathPaintedOnScreen = (flightPath.flightId === id && flightPath.polyLine !== null);

        // remove polyline
        if (!inSet && pathPaintedOnScreen) {
            flightPath.polyLine.setMap(null);
            initFlightPath();
        }

        // remove from details
        if (!inSet && inDetails) {
            document.getElementById("details_" + id).remove();
        }

        //THIRD: remove from table
        if (!inSet && inInternalOrExternal) {
            document.getElementById(id).remove();
        }
    });

}

function getFlights() {

    const currentTime = new Date().toISOString().substr(0, 19);
    const timeFormat = currentTime + 'Z';
    const ask = "/api/Flights?relative_to=" + timeFormat + "&sync_all";
    flightsIdsSet.clear();

    $.getJSON(ask, function (data) {
        data.forEach(function (flight) {
            flightsIdsSet.add(flight.flight_id);
            if (document.getElementById(flight.flight_id) === null) {
                if (!flight.is_external) {
                    let row = "<tr id=" + flight.flight_id + " onclick=showFlight('" + flight.flight_id + "') ><td>" + flight.flight_id
                        + "</td>" + "<td>" + flight.company_name + "</td>" + "<td>" + flight.date_time + "</td>"
                        + "<td><a id=a_" + flight.flight_id + " href='#'><i class='fa fa-trash' onclick=deleteFlight(\"" + flight.flight_id + "\")></i ></a>" + "</td></tr>";
                    $("#internalFlightsBody").append(row);
                    trashButton = document.getElementById("a_" + flight.flight_id);
                    trashButton.addEventListener("click", stopEvent, false);

                } else {
                    let externalRow = "<tr id=" + flight.flight_id + " onclick=showFlight('" + flight.flight_id + "') ><td>" + flight.flight_id
                        + "</td>" + "<td>" + flight.company_name + "</td>" + "<td>" + flight.date_time + "</td>";
                    $("#externalFlightsBody").append(externalRow);
                }
            }
        })
        removeInactiveFlights();
    });
}

function stopEvent(ev) {
    ev.stopPropagation();
}


function showFlight(flightID) {


    // remove green background of "table-success" from all internalFlights table and add it only to the selected row
    $("#internalFlights tr").removeClass('table-success');

    $("#externalFlights tr").removeClass('table-success');

    $('#' + flightID).addClass('table-success');


    // remove table flightPlanBody so the flight appear only once  
    $("#flightDetails > tbody").html("");

    // fill flightPlan table 
    const flightPlanAsk = "/api/FlightPlan/" + flightID;
    $.getJSON(flightPlanAsk, function (flightPlan) {
        // paint flight lines on map
        paintFlightPath(flightPlan, flightID);
        $("#flightDetailsBody").append("<tr id=details_" + flightID + "><td>" + flightID + "</td><td>"
            + flightPlan.company_name + "</td><td>" +
            flightPlan.passengers + "</td><td>" + "longitude: " + flightPlan.initial_location.longitude + " &emsp;"
            + "latitude: " + flightPlan.initial_location.latitude + "</td></tr>");
    })
        .fail(function (jqXHR) { toastr.error(jqXHR.statusText + ' : ' + jqXHR.responseText); })
}

function paintFlightPath(flightPlan, flightID) {
    // remove previous paths from map:
    if (flightPath.polyLine !== null) {
        flightPath.polyLine.setMap(null);
        initFlightPath();
    }

    let lines = [{ lat: Number(flightPlan.initial_location.latitude), lng: Number(flightPlan.initial_location.longitude) }];
    for (let i = 0; i < flightPlan.segments.length; i++) {
        const line = { lat: Number(flightPlan.segments[i].latitude), lng: Number(flightPlan.segments[i].longitude) };
        lines.push(line);
    }
    flightPath.polyLine = new google.maps.Polyline({
        path: lines,
        geodesic: true,
        strokeColor: '#4cff00',
        strokeOpacity: 1.0,
        strokeWeight: 2
    });
    flightPath.polyLine.setMap(map);
    flightPath.flightId = flightID;
}

const dropArea = document.getElementById('dropZone');

const preventDefaults = e => {
    e.preventDefault();
    e.stopPropagation();
};

const highlight = () => {
    dropArea.classList.add('highlight');
    $('#internalFlights').addClass('table-dark');
};

const unhighlight = () => {
    dropArea.classList.remove('highlight');
    $('#internalFlights').removeClass('table-dark');
};

const handleDrop = e => {
    const dt = e.dataTransfer;
    const file = dt.files;

    handleFiles(file);
};

const handleFiles = file => {
    if (file.length === 1 && file[0].type.includes('/json')) {
        let reader = new FileReader();

        reader.onload = function (e2) {
            // finished reading file data.
            let flightJson = atob(e2.target.result.replace('data:application/json;base64,', ''));

            // /api/FlightPlan
            $.ajax({
                url: '/api/FlightPlan',
                contentType: "application/json",
                type: "POST",
                data: flightJson
            })
                .done(function () {
                    location.reload();
                })
                .fail(function (jqXHR) {
                    toastr.error(jqXHR.statusText + ' : ' + jqXHR.responseText);
                });
        }

        reader.readAsDataURL(file[0]); // start reading the file data.
    } else {
        toastr.error("Accept Only 1 Json File");
    }
};

["dragenter", "dragover", "dragleave", "drop"].forEach(eventName => {
    dropArea.addEventListener(eventName, preventDefaults, false);
});

["dragenter", "dragover"].forEach(eventName => {
    dropArea.addEventListener(eventName, highlight, false);
});

["dragleave", "drop"].forEach(eventName => {
    dropArea.addEventListener(eventName, unhighlight, false)
});

dropArea.addEventListener("drop", handleDrop, false);

// deleteFlight used in writing text directly to the html file
function deleteFlight(id) {

    $.ajax({
        url: '/api/Flights/' + id,
        contentType: "text/plain; charset=utf-8",
        type: "DELETE",
    }).done(function () {
        let details = document.getElementById("details_" + id);
        if (details !== null) {
            details.remove();
        }
        document.getElementById(id).remove();
        if (flightPath.flightId === id) {
            flightPath.polyLine.setMap(null);
            initFlightPath();
        } else if (flightPath.flightId !== null) {
            //this case is relevant when the deleted flight is not the selected flight, so the selected flight need to remain selected.
            //showFlight(flightPath.flightId);
        }
    }).fail(function (jqXHR) {
        toastr.error(jqXHR.statusText + ' : ' + jqXHR.responseText);
    });
}

// the smooth zoom function
function smoothZoom(map, max, cnt) {
    if (cnt >= max) {
        return;
    }
    else {
        let z = google.maps.event.addListener(map, 'zoom_changed', function () {
            google.maps.event.removeListener(z);
            smoothZoom(map, max, cnt + 1);
        });
        setTimeout(function () { map.setZoom(cnt) }, 80); // 80ms is what I found to work well on my system -- it might not work well on all systems
    }
}



//The code itself:

// at beggining, get all flights
getFlights();

let intervalId;
$(document).ready(function () {
    intervalId = setInterval(function () {
        movePlanes();
        getFlights();
    }, 2000);
});

// At some other point
clearInterval(intervalId);
