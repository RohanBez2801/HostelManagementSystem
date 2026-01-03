async function loadUsers() {
    const container = document.getElementById('usersTableBody');
    if (!container) return;
    try {
        const res = await fetch('/api/Users/all');
        const users = await res.json();
        container.innerHTML = users.map(u => `
            <tr><td>${u.name}</td><td>${u.username}</td><td>${u.role}</td><td>${u.status}</td>
            <td><button class="btn-icon" style="color:red;" onclick="deleteUser(${u.id})"><i class="fas fa-trash"></i></button></td></tr>
        `).join('');
    } catch (err) { container.innerHTML = '<tr><td colspan="5">Error loading users</td></tr>'; }
}
async function saveUser() {
    // ... (Implementation from previous response) ...
}
async function deleteUser(id) {
    if (!confirm("Delete user?")) return;
    await fetch(`/api/Users/delete/${id}`, { method: 'DELETE' });
    loadUsers();
}