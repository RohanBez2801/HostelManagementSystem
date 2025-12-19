/**
 * LEARNER MANAGEMENT MODULE
 */

// 1. Fetch and Display Learners
async function loadLearners() {
    const tbody = document.getElementById('learnerTableBody');
    if (!tbody) return;

    try {
        const res = await fetch('/api/learner/list-all');
        const data = await res.json();

        tbody.innerHTML = data.map(s => `
            <tr>
                <td>${s.AdNo}</td>
                <td><strong>${s.Name}</strong></td>
                <td>Grade ${s.Grade}</td>
                <td>Room ${s.RoomID}</td>
                <td>
                    <button class="btn-icon" onclick="editLearner(${s.Id})"><i class="fas fa-edit"></i></button>
                    <button class="btn-icon" style="color:#ef4444" onclick="deleteLearner(${s.Id})"><i class="fas fa-trash"></i></button>
                    <button class="btn-icon" title="Statement" onclick="viewStatement(${s.Id}, '${s.Name}')"><i class="fas fa-file-invoice-dollar"></i></button>
                </td>
            </tr>
        `).join('');
    } catch (err) {
        console.error("Failed to load learners:", err);
    }
}

// 2. Submit New Registration
async function registerLearner() {
    const formData = {
        AdNo: document.getElementById('regAdNo').value,
        Name: document.getElementById('regName').value,
        Grade: document.getElementById('regGrade').value,
        RoomID: parseInt(document.getElementById('regRoomSelect').value)
    };

    try {
        const res = await fetch('/api/learner/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(formData)
        });

        if (res.ok) {
            alert("Learner registered successfully!");
            closeModal('regModal');
            loadLearners();
            updateStats(); // Refresh dashboard cards
        }
    } catch (err) {
        alert("Registration failed. Check console for details.");
    }
}

// 3. Delete Learner with Room Update
async function deleteLearner(id) {
    if (!confirm("Are you sure? This will also update the room occupancy.")) return;

    try {
        const res = await fetch(`/api/learner/delete/${id}`, { method: 'DELETE' });
        if (res.ok) {
            loadLearners();
            updateStats();
        }
    } catch (err) {
        console.error("Delete failed:", err);
    }
}