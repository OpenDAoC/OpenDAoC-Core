<?php
include_once('fix_mysql.inc.php');

// Configuration
$db = mysql_connect(getenv('ATLAS_STATS_DB_ADDRESS'), getenv('ATLAS_STATS_USER'), getenv('ATLAS_STATS_PSW')) or die('Can\'t connect to database: ' . $db);
mysql_select_db(getenv('ATLAS_STATS_DB'), $db) or die('Can\'t select database: ' . mysql_error($db));

// Utils
function Query($query)
{
    global $db;

    $result = mysql_query($query, $db) or die("Error mysql ($query): " . mysql_error($db));
    return $result;
}

function GetPlayedTime($time)
{
    $sec = $time%60;
    $time = ($time-$sec)/60;
    $min = $time%60;
    $time = ($time-$min)/60;
    $hours = $time%24;
    $time = ($time-$hours)/24;
    $day = $time%365;
    $time = $time-$day;
    $year = round($time/365);
    return ($year > 0 ? $year.'year(s) ' : '').($day > 0 ? $day.'day(s) ' : '').$hours.'h'.$min.'m'.$sec.'s';
}

// General informations
$numacc = Query('SELECT Count(*) FROM `account`');
$numactiveacc = Query('SELECT Count(*) FROM `account` WHERE UNIX_TIMESTAMP( `LastLogin` ) >= ( UNIX_TIMESTAMP( ) -2592000 )');
$numchar = Query('SELECT Count(*), Sum(Level), Sum(`PlayedTime`) FROM `dolcharacters`');
$numactivechar = Query('SELECT Count(*), Sum(Level) FROM `dolcharacters` WHERE UNIX_TIMESTAMP( `LastPlayed` ) >= ( UNIX_TIMESTAMP( ) -2592000 )');

$nbActivePlayers = mysql_result($numactivechar, 0);
$nbPlayers = mysql_result($numchar, 0);

$stats = array();
$stats[] = 'Accounts (actives / total): '.mysql_result($numactiveacc, 0).' / '.mysql_result($numacc, 0);
$stats[] = 'Characters (actives / total): '.$nbActivePlayers.' / '.$nbPlayers;
$stats[] = 'Average levels (actives / total): '.intval(mysql_result($numactivechar, 0, 1) / $nbActivePlayers).' / '.intval(mysql_result($numchar, 0, 1) / $nbPlayers);
$stats[] = 'Total played time (minus deleted characters): '.GetPlayedTime(mysql_result($numchar, 0, 2));

$dateEnd = time();
$dateStart = $dateEnd - (2 * 24 * 60 *  60); // 2 days
?>
<!DOCTYPE html>
<html style="width: 100%; height: 100%">
<head>
    <meta http-equiv="content-type" content="text/html; charset=UTF-8">
    <meta charset="utf-8">
    <title>Atlas Freeshard - Statistics</title>
    <link rel="icon" type="image/svg+xml" href="../img/favicon.svg">
    <meta name="viewport" content="width=device-width, maximum-scale=1.0, initial-scale=1.0">
    <style>
        body {
          font: 10px sans-serif;
        }

        .axis path,
        .axis line {
          fill: none;
          stroke: #000;
          shape-rendering: crispEdges;
        }

        .x.axis path {
          display: none;
        }

        .line {
          fill: none;
          stroke: steelblue;
          stroke-width: 1.5px;
        }
    </style>
</head>
<body style="width: 100%; height: 100%">
    <h2>Atlas Freeshard - Statistics</h2>
    <ul>
        <?php foreach ($stats as $v) { ?>
            <li><?php echo($v); ?></li>
        <?php } ?>
    </ul>
    <div id="players"></div>
    <div id="cpu"></div>
    <div id="memory"></div>
    <div id="download"></div>
    <div id="upload"></div>
    <script src="http://d3js.org/d3.v3.min.js" charset="utf-8"></script>
    <script>
        (function() {
            var data = [
<?php
                // Graphs
                $req = Query('SELECT UNIX_TIMESTAMP(StatDate), Clients, CPU, Memory, Upload, Download FROM serverstats WHERE `statdate` >= FROM_UNIXTIME( \''.mysql_real_escape_string($dateStart).'\') AND `statdate` <= FROM_UNIXTIME( \''.mysql_real_escape_string($dateEnd).'\') ORDER BY `StatDate` ASC ');
                while($row = mysql_fetch_array($req))
                    echo('[new Date(' . $row[0] . '*1000),' . $row[1] . ',' . $row[2]  . ','  . $row[3]  . ','  . $row[4] . ',' . $row[5] . "],\n");
?>
            ];

            var buildGraph = function(desc, div, index, calc) {
                var margin = {top: 20, right: 20, bottom: 30, left: 50},
                    width = 960 - margin.left - margin.right,
                    height = 300 - margin.top - margin.bottom;
                
                var x = d3.time.scale().range([0, width]);
                var y = d3.scale.linear().range([height, 0]);
                
                var xAxis = d3.svg.axis()
                    .scale(x)
                    .orient('bottom');
                var yAxis = d3.svg.axis()
                    .scale(y)
                    .orient('left');

                var line = d3.svg.line()
                if (typeof (calc) === 'function') {
                    line.x(function(r) { return x(r[0]); })
                        .y(function(r) { return y(calc(r[index])); });
                } else {
                    line.x(function(r) { return x(r[0]); })
                        .y(function(r) { return y(r[index]); });
                }

                var svg = d3.select(div).append('svg')
                    .attr('width', width + margin.left + margin.right)
                    .attr('height', height + margin.top + margin.bottom)
                    .append('g')
                    .attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');

                x.domain(d3.extent(data, function(d) { return d[0]; }));
                if (typeof (calc) === 'function') {
                    y.domain(d3.extent(data, function(d) { return calc(d[index]); }));
                } else {
                    y.domain(d3.extent(data, function(d) { return d[index]; }));
                }

                svg.append('g')
                    .attr('class', 'x axis')
                    .attr('transform', 'translate(0,' + height + ')')
                    .call(xAxis);

                svg.append('g')
                        .attr('class', 'y axis')
                        .call(yAxis)
                    .append('text')
                        .attr('transform', 'rotate(-90)')
                        .attr('y', 6)
                        .attr('dy', '.71em')
                        .style('text-anchor', 'end')
                        .text(desc);

                svg.append('path')
                    .datum(data)
                    .attr('class', 'line')
                    .attr('d', line);
            };

            buildGraph('Clients', '#players', 1);
            buildGraph('CPU Usage (%)', '#cpu', 2);
            buildGraph('Memory Usage (in Mbytes)', '#memory', 3, function (s) { return s / 1024; });
            buildGraph('Upload (in Kbytes)', '#upload', 4);
            buildGraph('Download (in Kbytes)', '#download', 5);
        })();
    </script>
    <p>An active account is an account connected at least one time in the last 30 days.</p>
</body>
</html>
