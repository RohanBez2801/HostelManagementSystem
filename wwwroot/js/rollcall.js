// js/rollcall.js (or integrated into reports.js)

function printRollCall() {
    // 1. Fetch all learners
    fetch('/api/learner/all')
        .then(res => res.json())
        .then(data => {
            // 2. Sort by Room (Block/Room)
            data.sort((a, b) => {
                const roomA = a.room || "ZZZ"; // Put unassigned last
                const roomB = b.room || "ZZZ";
                return roomA.localeCompare(roomB, undefined, {numeric: true, sensitivity: 'base'});
            });

            // 3. Generate HTML
            const win = window.open('', '_blank');
            const d = new Date();
            const dateStr = d.toLocaleDateString() + " (Week " + getWeekNumber(d) + ")";
            
            let html = `
            <html>
            <head>
                <title>Hostel Roll Call Sheet</title>
                <style>
                    body { font-family: sans-serif; padding: 20px; }
                    h1 { text-align: center; margin-bottom: 5px; }
                    h3 { text-align: center; margin-top: 0; color: #555; }
                    table { width: 100%; border-collapse: collapse; margin-top: 20px; font-size: 12px; }
                    th, td { border: 1px solid #000; padding: 5px; text-align: left; }
                    th { background-color: #eee; }
                    .center { text-align: center; }
                    .check-col { width: 40px; }
                    @media print {
                        .no-print { display: none; }
                        table { page-break-inside: auto; }
                        tr { page-break-inside: avoid; page-break-after: auto; }
                    }
                </style>
            </head>
            <body>
                <h1>HOSTEL ROLL CALL</h1>
                <h3>${dateStr}</h3>
                <table>
                    <thead>
                        <tr>
                            <th style="width: 80px;">Room</th>
                            <th>Name</th>
                            <th style="width: 50px;">Grade</th>
                            <th class="center check-col">Mon</th>
                            <th class="center check-col">Tue</th>
                            <th class="center check-col">Wed</th>
                            <th class="center check-col">Thu</th>
                            <th class="center check-col">Fri</th>
                            <th class="center check-col">Sat</th>
                            <th class="center check-col">Sun</th>
                        </tr>
                    </thead>
                    <tbody>
            `;

            data.forEach(s => {
                html += `
                    <tr>
                        <td><b>${s.room || '-'}</b></td>
                        <td>${s.surname}, ${s.names}</td>
                        <td class="center">${s.grade}</td>
                        <td></td><td></td><td></td><td></td><td></td><td></td><td></td>
                    </tr>
                `;
            });

            html += `
                    </tbody>
                </table>
                <div class="no-print" style="margin-top: 20px; text-align: center;">
                    <button onclick="window.print()" style="padding: 10px 20px; font-size: 16px; cursor: pointer;">PRINT LIST</button>
                </div>
            </body>
            </html>
            `;

            // 4. Write to window and print
            win.document.write(html);
            win.document.close();
        })
        .catch(err => {
            console.error(err);
            alert('Failed to generate roll call list.');
        });
}

// Helper for week number
function getWeekNumber(d) {
    d = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
    d.setUTCDate(d.getUTCDate() + 4 - (d.getUTCDay()||7));
    var yearStart = new Date(Date.UTC(d.getUTCFullYear(),0,1));
    var weekNo = Math.ceil(( ( (d - yearStart) / 86400000) + 1)/7);
    return weekNo;
}
