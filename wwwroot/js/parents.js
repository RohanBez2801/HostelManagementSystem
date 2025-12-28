// js/parents.js

function loadParents() {
    fetch('/api/parent/all')
        .then(res => res.json())
        .then(data => {
            const tbody = document.getElementById('parentTableBody');
            tbody.innerHTML = '';
            data.forEach(p => {
                const row = `<tr>
                    <td>${p.name}</td>
                    <td>${p.phone}</td>
                    <td>${p.email}</td>
                    <td>
                        <button class="btn-sm btn-primary" onclick="viewChildren(${p.id}, '${p.name}')">View Children</button>
                    </td>
                </tr>`;
                tbody.innerHTML += row;
            });
        })
        .catch(err => console.error(err));
}

function openAddParentModal() {
    document.getElementById('addParentModal').style.display = 'flex';
}

function closeAddParentModal() {
    document.getElementById('addParentModal').style.display = 'none';
}

function saveParent() {
    const parent = {
        name: document.getElementById('parName').value,
        phone: document.getElementById('parPhone').value,
        email: document.getElementById('parEmail').value,
        address: document.getElementById('parAddress').value
    };

    fetch('/api/parent/add', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(parent)
    })
    .then(res => {
        if (res.ok) {
            closeAddParentModal();
            loadParents();
            alert('Parent added!');
        } else {
            alert('Error adding parent');
        }
    });
}

// Linking Logic
let currentParentId = 0;

function viewChildren(parentId, parentName) {
    currentParentId = parentId;
    document.getElementById('linkParentName').innerText = parentName;
    document.getElementById('linkChildModal').style.display = 'flex';
    loadLinkedChildren(parentId);
    loadUnlinkedLearners();
}

function closeLinkModal() {
    document.getElementById('linkChildModal').style.display = 'none';
}

function loadLinkedChildren(parentId) {
    fetch(`/api/parent/children/${parentId}`)
        .then(res => res.json())
        .then(data => {
            const list = document.getElementById('linkedChildrenList');
            list.innerHTML = '';
            if (data.length === 0) {
                list.innerHTML = '<li>No children linked yet.</li>';
                return;
            }
            data.forEach(c => {
                list.innerHTML += `<li>${c.name} (Grade ${c.grade})</li>`;
            });
        });
}

function loadUnlinkedLearners() {
    fetch('/api/learner/all')
        .then(res => res.json())
        .then(data => {
            const sel = document.getElementById('linkChildSelect');
            sel.innerHTML = '<option value="">-- Select Student to Link --</option>';
            data.forEach(s => {
                // Ideally filter out those already linked to THIS parent, but for now just list all
                sel.innerHTML += `<option value="${s.id}">${s.surname} ${s.names} (Grade ${s.grade})</option>`;
            });
        });
}

function linkChild() {
    const learnerId = document.getElementById('linkChildSelect').value;
    if (!learnerId) return;

    fetch('/api/parent/link', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ parentId: currentParentId, learnerId: parseInt(learnerId) })
    })
    .then(res => {
        if (res.ok) {
            loadLinkedChildren(currentParentId);
            alert('Child linked successfully.');
        } else {
            alert('Error linking child');
        }
    });
}
