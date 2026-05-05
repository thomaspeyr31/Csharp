// Screen routing + workspace and board listing.
const Views = (() => {
    const screens = ['screen-auth', 'screen-workspaces', 'screen-boards', 'screen-board'];
    let currentWorkspaceId = null;
    let currentBoardId = null;

    function show(id) {
        screens.forEach(s => document.getElementById(s).hidden = s !== id);
        const topbar = document.getElementById('topbar');
        topbar.hidden = id === 'screen-auth';
        document.getElementById('me-email').textContent = Api.getEmail() || '';
    }

    function setCrumb(wsName, boardName) {
        const wsCrumb = document.getElementById('crumb-ws');
        const boardCrumb = document.getElementById('crumb-board');
        wsCrumb.hidden = !wsName;
        boardCrumb.hidden = !boardName;
        if (wsName) wsCrumb.querySelector('strong').textContent = wsName;
        if (boardName) boardCrumb.querySelector('strong').textContent = boardName;
    }

    async function showWorkspaces() {
        currentWorkspaceId = null;
        currentBoardId = null;
        setCrumb(null, null);
        const list = document.getElementById('workspaces-list');
        list.innerHTML = '';
        try {
            const wss = await Api.getWorkspaces();

            const owned = wss.filter(w => w.myRole === 'Owner').length;
            const invited = wss.length - owned;
            const summary = document.getElementById('workspaces-summary');
            if (summary) {
                summary.textContent = wss.length === 0
                    ? "Tu n'as encore aucun workspace."
                    : `${owned} créé${owned > 1 ? 's' : ''} par toi · ${invited} où tu es invité${invited > 1 ? 's' : ''}.`;
            }

            wss.forEach(ws => {
                const li = document.createElement('li');
                const isOwner = ws.myRole === 'Owner';
                const removeBtn = isOwner
                    ? `<span class="actions"><button data-id="${ws.id}" title="Supprimer">×</button></span>`
                    : '';
                li.innerHTML = `<div>
                                    <span>${escapeHtml(ws.name)}</span>
                                    <span class="role ${isOwner ? 'owner' : ''}">${isOwner ? 'Propriétaire' : 'Membre'}</span>
                                </div>
                                ${removeBtn}`;
                li.addEventListener('click', e => {
                    if (e.target.tagName === 'BUTTON') return;
                    showBoards(ws);
                });
                if (isOwner) {
                    li.querySelector('button').addEventListener('click', async (e) => {
                        e.stopPropagation();
                        if (!confirm(`Supprimer le workspace "${ws.name}" ?`)) return;
                        try { await Api.deleteWorkspace(ws.id); showWorkspaces(); }
                        catch (err) { alert(err.message); }
                    });
                }
                list.appendChild(li);
            });
        } catch (err) {
            list.innerHTML = `<li class="error">${err.message}</li>`;
        }
        show('screen-workspaces');
    }

    async function showBoards(ws) {
        currentWorkspaceId = ws.id;
        currentBoardId = null;
        setCrumb(ws.name, null);
        document.getElementById('boards-title').textContent = `Boards — ${ws.name}`;
        const list = document.getElementById('boards-list');
        list.innerHTML = '';
        try {
            const all = await Api.getBoards();
            all.filter(b => b.workspaceId === ws.id).forEach(b => {
                const li = document.createElement('li');
                li.innerHTML = `<span>${escapeHtml(b.name)}</span>
                                <span class="actions"><button data-id="${b.id}" title="Supprimer">×</button></span>`;
                li.addEventListener('click', e => {
                    if (e.target.tagName === 'BUTTON') return;
                    Board.open(b);
                });
                li.querySelector('button').addEventListener('click', async (e) => {
                    e.stopPropagation();
                    if (!confirm(`Supprimer le board "${b.name}" ?`)) return;
                    try { await Api.deleteBoard(b.id); showBoards(ws); }
                    catch (err) { alert(err.message); }
                });
                list.appendChild(li);
            });
        } catch (err) {
            list.innerHTML = `<li class="error">${err.message}</li>`;
        }

        await renderMembers(ws);

        show('screen-boards');
    }

    async function renderMembers(ws) {
        const ul = document.getElementById('members-list');
        const inviteForm = document.getElementById('form-invite-member');
        ul.innerHTML = '';
        try {
            const members = await Api.getWorkspaceMembers(ws.id);
            const meEmail = Api.getEmail();
            const isOwner = members.some(m => m.email === meEmail && m.role === 'Owner');
            inviteForm.hidden = !isOwner;

            members.forEach(m => {
                const li = document.createElement('li');
                const removeBtn = (isOwner && m.role !== 'Owner')
                    ? `<button class="remove" data-uid="${m.userId}" title="Retirer">×</button>` : '';
                li.innerHTML = `<span>
                                    ${escapeHtml(m.username)}
                                    <span class="role ${m.role === 'Owner' ? 'owner' : ''}">${m.role}</span>
                                    <small style="color:#5e6c84"> · ${escapeHtml(m.email)}</small>
                                </span>
                                ${removeBtn}`;
                const btn = li.querySelector('.remove');
                if (btn) {
                    btn.addEventListener('click', async () => {
                        if (!confirm(`Retirer ${m.username} du workspace ?`)) return;
                        try { await Api.removeWorkspaceMember(ws.id, m.userId); renderMembers(ws); }
                        catch (err) { alert(err.message); }
                    });
                }
                ul.appendChild(li);
            });
        } catch (err) {
            ul.innerHTML = `<li class="error">${err.message}</li>`;
            inviteForm.hidden = true;
        }
    }

    function escapeHtml(s) {
        return String(s).replace(/[&<>"']/g, c => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        })[c]);
    }

    return {
        show, setCrumb, showWorkspaces, showBoards, escapeHtml,
        getWorkspaceId: () => currentWorkspaceId,
        getBoardId: () => currentBoardId,
        setBoardId: (id) => { currentBoardId = id; },
    };
})();

