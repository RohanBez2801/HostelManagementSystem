// NOTICE BOARD MODULE

async function loadNotices() {
    const container = document.getElementById('noticeList');
    if (!container) return;

    container.innerHTML = '<p style="color:#94a3b8; font-size:12px;">Loading notices...</p>';

    try {
        const res = await fetch('/api/Notice/all');
        const data = await res.json();

        if (data.length === 0) {
            container.innerHTML = '<p style="color:#94a3b8; font-size:13px;">No new announcements.</p>';
            return;
        }

        container.innerHTML = data.map(n => {
            const color = n.priority === 'High' ? '#ef4444' : '#3b82f6';
            const bg = n.priority === 'High' ? '#fef2f2' : '#eff6ff';

            return `
                <div style="background:${bg}; border-left:4px solid ${color}; padding:10px; margin-bottom:10px; border-radius:4px; position:relative;">
                    <div style="font-size:14px; color:#1e293b; margin-bottom:4px;">${n.message}</div>
                    <div style="font-size:11px; color:#64748b; display:flex; justify-content:space-between;">
                        <span>${n.date}</span>
                        <button onclick="deleteNotice(${n.id})" style="border:none; background:none; color:#94a3b8; cursor:pointer;" title="Delete">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </div>
            `;
        }).join('');

    } catch (err) {
        console.error(err);
        container.innerHTML = '<p style="color:red;">Failed to load notices.</p>';
    }
}

async function postNotice() {
    const input = document.getElementById('noticeInput');
    const priority = document.getElementById('noticePriority');

    if (!input.value) return alert("Please type a message.");

    const btn = document.getElementById('btnPostNotice');
    btn.innerText = "...";
    btn.disabled = true;

    try {
        const res = await fetch('/api/Notice/add', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                Message: input.value,
                Priority: priority.value
            })
        });

        if (res.ok) {
            input.value = "";
            loadNotices();
        }
    } catch (err) {
        alert("Error posting notice");
    } finally {
        btn.innerHTML = '<i class="fas fa-paper-plane"></i>';
        btn.disabled = false;
    }
}

async function deleteNotice(id) {
    if (!confirm("Delete this notice?")) return;
    await fetch(`/api/Notice/delete/${id}`, { method: 'DELETE' });
    loadNotices();
}