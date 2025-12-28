/**
 * ATTENDANCE & LEAVE MODULE
 */

async function loadAttendance() {
    const tbody = document.getElementById('attendanceTableBody');
    const dateInput = document.getElementById('attendanceDate');
    
    if (!dateInput.value) {
        dateInput.valueAsDate = new Date();
    }
    
    const date = dateInput.value;

    try {
        // 1. Get all learners
        const learnerRes = await fetch('/api/learner/list-all');
        const learnerData = await learnerRes.json();
        if (!learnerRes.ok) throw new Error(learnerData.Message || learnerData.message || "Failed to load students");
        const learners = Array.isArray(learnerData) ? learnerData : (learnerData.value || []);

        // 2. Get attendance for selected date
        const attendanceRes = await fetch(`/api/attendance/list?date=${date}`);
        const attendanceData = await attendanceRes.json();
        // Attendance might return empty or null if no records yet, that's fine, but should check if it crashed
        if (!attendanceRes.ok) throw new Error(attendanceData.Message || attendanceData.message || "Failed to load attendance");
        const attendanceRecords = Array.isArray(attendanceData) ? attendanceData : (attendanceData.value || []);

        tbody.innerHTML = learners.map(s => {
            const record = attendanceRecords.find(r => r.learnerId === s.id);
            const status = record ? record.status : 'Present'; // Default to Present

            return `
                <tr>
                    <td>${s.adNo}</td>
                    <td><strong>${s.name}</strong></td>
                    <td>Grade ${s.grade}</td>
                    <td>Room ${s.room}</td>
                    <td>
                        <select data-learner-id="${s.id}" class="attendance-status" style="padding:4px; border-radius:4px;">
                            <option value="Present" ${status === 'Present' ? 'selected' : ''}>Present</option>
                            <option value="Leave" ${status === 'Leave' ? 'selected' : ''}>On Leave</option>
                            <option value="Absent" ${status === 'Absent' ? 'selected' : ''}>Absent</option>
                        </select>
                    </td>
                </tr>
            `;
        }).join('');
    } catch (err) {
        console.error("Failed to load attendance:", err);
    }
}

async function saveAttendance() {
    const date = document.getElementById('attendanceDate').value;
    const statuses = document.querySelectorAll('.attendance-status');
    const records = [];

    statuses.forEach(select => {
        records.push({
            LearnerId: parseInt(select.dataset.learnerId),
            Status: select.value,
            Date: date
        });
    });

    try {
        const res = await fetch('/api/attendance/bulk-save', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(records)
        });

        if (res.ok) {
            alert("Attendance saved successfully!");
        } else {
            alert("Failed to save attendance.");
        }
    } catch (err) {
        console.error("Save attendance error:", err);
        alert("Error saving attendance.");
    }
}
