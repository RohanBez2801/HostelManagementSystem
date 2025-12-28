/**
 * DISCIPLINE TRACKING MODULE
 */

async function loadDisciplineLearners() {
    const select = document.getElementById('discStudentSelect');
    const entrySelect = document.getElementById('discEntryStudentSelect');
    if (!select) return;

    try {
        const res = await fetch('/api/learner/list-all');
        const data = await res.json();
        
        if (!res.ok) throw new Error(data.Message || data.message || "Failed to load students");
        const students = Array.isArray(data) ? data : (data.value || []);
        
        const options = '<option value="">-- Choose Learner --</option>' +
            students.map(s => `<option value="${s.id}">${s.name} (${s.adNo})</option>`).join('');
        
        select.innerHTML = options;
        if (entrySelect) entrySelect.innerHTML = options;
    } catch (err) {
        console.error("Error loading students for discipline:", err);
    }
}

async function loadDisciplineHistory(learnerId) {
    if (!learnerId) {
        document.getElementById('disciplineTableBody').innerHTML = '';
        return;
    }

    try {
        const res = await fetch(`/api/discipline/history/${learnerId}`);
        const data = await res.json();
        
        if (!res.ok) throw new Error(data.Message || data.message || "Failed to load history");
        const list = Array.isArray(data) ? data : (data.value || []);

        document.getElementById('disciplineTableBody').innerHTML = list.map(item => `
            <tr>
                <td>${new Date(item.date).toLocaleDateString()}</td>
                <td>${item.text}</td>
                <td><span class="badge" style="background:${getSeverityColor(item.level)}">${item.level}</span></td>
                <td>${item.by}</td>
            </tr>
        `).join('');
    } catch (err) {
        console.error("Failed to load history:", err);
    }
}

function getSeverityColor(level) {
    switch (level) {
        case 'Minor': return '#10b981';
        case 'Moderate': return '#f59e0b';
        case 'Severe': return '#ef4444';
        default: return '#64748b';
    }
}

function openDisciplineModal() {
    document.getElementById('disciplineModal').style.display = 'flex';
}

function closeDisciplineModal() {
    document.getElementById('disciplineModal').style.display = 'none';
    document.getElementById('disciplineForm').reset();
}

async function saveDisciplineIncident() {
    const payload = {
        LearnerId: parseInt(document.getElementById('discEntryStudentSelect').value),
        Description: document.getElementById('discDesc').value,
        Severity: document.getElementById('discSeverity').value,
        ReportedBy: document.getElementById('discReporter').value
    };

    if (!payload.LearnerId || !payload.Description) {
        alert("Please fill in all fields.");
        return;
    }

    try {
        const res = await fetch('/api/discipline/log', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            alert("Incident logged.");
            closeDisciplineModal();
            // If the current view is for this student, refresh it
            const currentSelected = document.getElementById('discStudentSelect').value;
            if (currentSelected == payload.LearnerId) {
                loadDisciplineHistory(currentSelected);
            }
        }
    } catch (err) {
        alert("Failed to log incident.");
    }
}
