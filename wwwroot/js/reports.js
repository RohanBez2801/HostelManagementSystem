/**
 * REPORTS MODULE
 */

async function generateFinancialReport() {
    const currency = typeof getCurrency === 'function' ? getCurrency() : "N$";
    try {
        const res = await fetch('/api/Financials/summary');
        const data = await res.json();
        
        if (!res.ok) {
            throw new Error(data.Message || data.message || "Server Error");
        }
        
        let html = `
            <div id="printArea">
                <div style="text-align:center; border-bottom:2px solid #333; padding-bottom:10px; margin-bottom:20px;">
                    <h2 style="margin:0">Annual Financial Summary Report</h2>
                    <p style="margin:5px 0; color:#666;">Year: ${new Date().getFullYear()}</p>
                </div>
                
                <div style="display:grid; grid-template-columns: repeat(3, 1fr); gap:20px; margin-bottom:30px;">
                    <div style="padding:15px; background:#f0f9ff; border-radius:8px; border:1px solid #bae6fd;">
                        <div style="font-size:12px; color:#0369a1; font-weight:700;">TOTAL COLLECTED</div>
                        <div style="font-size:20px; font-weight:800;">${currency} ${(data.total || 0).toFixed(2)}</div>
                    </div>
                    <div style="padding:15px; background:#ecfdf5; border-radius:8px; border:1px solid #a7f3d0;">
                        <div style="font-size:12px; color:#047857; font-weight:700;">MOE PORTION</div>
                        <div style="font-size:20px; font-weight:800;">${currency} ${(data.moe || 0).toFixed(2)}</div>
                    </div>
                    <div style="padding:15px; background:#fff7ed; border-radius:8px; border:1px solid #fed7aa;">
                        <div style="font-size:12px; color:#c2410c; font-weight:700;">HDF PORTION</div>
                        <div style="font-size:20px; font-weight:800;">${currency} ${(data.hdf || 0).toFixed(2)}</div>
                    </div>
                </div>

                <p style="font-size:14px; line-height:1.6; color:#444;">
                    This report summarizes all hostel fees collected for the current financial year. 
                    The MoE portion represents funds allocated for the Ministry of Education, 
                    while the HDF portion represents the Hostel Development Fund for local maintenance and improvements.
                </p>

                <div style="margin-top:50px; display:flex; justify-content:space-between; border-top:1px solid #eee; padding-top:20px;">
                    <div style="text-align:center; width:200px;">
                        <div style="border-bottom:1px solid #333; height:40px;"></div>
                        <p style="font-size:12px; margin-top:5px;">Hostel Superintendent</p>
                    </div>
                    <div style="text-align:center; width:200px;">
                        <div style="border-bottom:1px solid #333; height:40px;"></div>
                        <p style="font-size:12px; margin-top:5px;">Date</p>
                    </div>
                </div>
            </div>
            <div style="margin-top:30px; display:flex; gap:10px;">
                <button onclick="window.print()" class="btn-submit"><i class="fas fa-print"></i> Print Report</button>
                <button onclick="document.getElementById('dynamicModal').remove()" class="btn-logout" style="width:auto; margin:0;">Close</button>
            </div>
        `;
        openModalContent(html);
    } catch (err) {
        alert("Failed to generate report: " + err.message);
    }
}

async function generateOccupancyReport() {
    try {
        const res = await fetch('/api/Room/all');
        const data = await res.json();
        
        if (!res.ok) {
            throw new Error(data.Message || data.message || "Server Error");
        }

        const rooms = Array.isArray(data) ? data : (data.value || []);
        
        if (rooms.length === 0 && !Array.isArray(data)) {
             // If we got an object but no 'value' array, and it's not an array itself
             console.warn("Expected array of rooms but got:", data);
        }

        let totalCap = 0;
        let totalOcc = 0;
        rooms.forEach(r => {
            const cap = r.capacity ?? r.Capacity ?? 0;
            const occ = r.occupied ?? r.Occupied ?? 0;
            totalCap += cap;
            totalOcc += occ;
        });

        const percentage = totalCap > 0 ? ((totalOcc / totalCap) * 100).toFixed(1) : 0;

        let html = `
            <div id="printArea">
                <div style="text-align:center; border-bottom:2px solid #333; padding-bottom:10px; margin-bottom:20px;">
                    <h2 style="margin:0">Hostel Occupancy & Capacity Report</h2>
                    <p style="margin:5px 0; color:#666;">Generated on: ${new Date().toLocaleDateString()}</p>
                </div>

                <div style="background:#f8fafc; padding:20px; border-radius:12px; margin-bottom:25px; border:1px solid #e2e8f0; text-align:center;">
                    <div style="font-size:14px; color:#64748b; font-weight:600; text-transform:uppercase;">Overall Occupancy Rate</div>
                    <div style="font-size:48px; font-weight:800; color:var(--primary);">${percentage}%</div>
                    <div style="font-size:14px; color:#64748b;">${totalOcc} Beds Occupied / ${totalCap} Total Capacity</div>
                </div>

                <table style="width:100%; border-collapse: collapse;">
                    <thead>
                        <tr style="background:#f1f5f9;">
                            <th style="padding:10px; border:1px solid #e2e8f0; text-align:left;">Room Number</th>
                            <th style="padding:10px; border:1px solid #e2e8f0; text-align:left;">Block</th>
                            <th style="padding:10px; border:1px solid #e2e8f0; text-align:center;">Capacity</th>
                            <th style="padding:10px; border:1px solid #e2e8f0; text-align:center;">Occupied</th>
                            <th style="padding:10px; border:1px solid #e2e8f0; text-align:center;">Available</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${rooms.map(r => {
                            const num = r.number ?? r.Number ?? r.roomNumber ?? r.RoomNumber ?? 'N/A';
                            const block = r.block ?? r.Block ?? r.blockName ?? r.BlockName ?? '-';
                            const cap = r.capacity ?? r.Capacity ?? 0;
                            const occ = r.occupied ?? r.Occupied ?? 0;
                            const avail = r.available ?? r.Available ?? (cap - occ);
                            
                            return `
                                <tr>
                                    <td style="padding:10px; border:1px solid #e2e8f0;">Room ${num}</td>
                                    <td style="padding:10px; border:1px solid #e2e8f0;">${block}</td>
                                    <td style="padding:10px; border:1px solid #e2e8f0; text-align:center;">${cap}</td>
                                    <td style="padding:10px; border:1px solid #e2e8f0; text-align:center;">${occ}</td>
                                    <td style="padding:10px; border:1px solid #e2e8f0; text-align:center; font-weight:bold; color:${avail === 0 ? '#ef4444' : '#10b981'}">${avail}</td>
                                </tr>
                            `;
                        }).join('')}
                    </tbody>
                </table>
            </div>
            <div style="margin-top:30px; display:flex; gap:10px;">
                <button onclick="window.print()" class="btn-submit"><i class="fas fa-print"></i> Print Report</button>
                <button onclick="document.getElementById('dynamicModal').remove()" class="btn-logout" style="width:auto; margin:0;">Close</button>
            </div>
        `;
        openModalContent(html);
    } catch (err) {
        alert("Failed to generate report: " + err.message);
    }
}
