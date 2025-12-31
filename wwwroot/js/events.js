// EVENTS MODULE

async function loadEvents() {
    const container = document.getElementById('eventsContainer');
    if (!container) return;

    container.innerHTML = '<p style="text-align:center;">Loading events...</p>';

    try {
        const res = await fetch('/api/Events/all');
        const data = await res.json();

        if (data.length === 0) {
            container.innerHTML = '<div class="fancy-card" style="text-align:center; color:#94a3b8;">No upcoming events.</div>';
            return;
        }

        container.innerHTML = `<div class="grid-3">` + data.map(e => `
            <div class="fancy-card" style="border-top:4px solid var(--primary);">
                <div class="flex-between">
                    <span style="font-size:12px; color:#64748b; font-weight:bold;">${e.date}</span>
                    <span class="badge" style="background:#f1f5f9;">${e.type}</span>
                </div>
                <h3 style="margin:10px 0; font-size:16px;">${e.title}</h3>
                <p style="color:#4b5563; font-size:13px; margin-bottom:15px;">${e.description}</p>
                <button onclick="deleteEvent(${e.id})" style="color:var(--danger); background:none; border:none; cursor:pointer; font-size:12px;">Delete</button>
            </div>
        `).join('') + `</div>`;
    } catch (err) { container.innerHTML = '<p style="color:red; text-align:center;">Error loading events.</p>'; }
}

async function saveEvent() {
    const title = document.getElementById('evtTitle').value;
    const date = document.getElementById('evtDate').value;
    const type = document.getElementById('evtType').value;
    const desc = document.getElementById('evtDesc').value;

    if (!title || !date) return alert("Title and Date are required.");

    try {
        await fetch('/api/Events/add', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Title: title, Date: date, Type: type, Description: desc })
        });

        document.getElementById('eventForm').reset();
        document.getElementById('eventModal').style.display = 'none';
        loadEvents();
    } catch (e) { alert("Failed to save event"); }
}

async function deleteEvent(id) {
    if (!confirm("Delete this event?")) return;
    await fetch(`/api/Events/delete/${id}`, { method: 'DELETE' });
    loadEvents();
}
