// App entry point: wire DOM events.
document.addEventListener('DOMContentLoaded', () => {

    // Auth tabs
    document.querySelectorAll('.tabs .tab').forEach(btn => {
        btn.addEventListener('click', () => {
            const target = btn.dataset.tab;
            document.querySelectorAll('.tabs .tab').forEach(t => t.classList.toggle('active', t === btn));
            document.getElementById('form-login').hidden = target !== 'login';
            document.getElementById('form-register').hidden = target !== 'register';
        });
    });

    document.getElementById('form-login').addEventListener('submit', async e => {
        e.preventDefault();
        const f = e.target;
        const err = document.getElementById('login-error');
        err.hidden = true;
        try {
            await Api.login(f.email.value, f.password.value);
            await Views.showWorkspaces();
        } catch (ex) {
            err.textContent = ex.message;
            err.hidden = false;
        }
    });

    document.getElementById('form-register').addEventListener('submit', async e => {
        e.preventDefault();
        const f = e.target;
        const err = document.getElementById('register-error');
        const ok = document.getElementById('register-ok');
        err.hidden = true; ok.hidden = true;
        try {
            await Api.register(f.username.value, f.email.value, f.password.value);
            ok.textContent = 'Compte créé. Tu peux maintenant te connecter.';
            ok.hidden = false;
            f.reset();
        } catch (ex) {
            err.textContent = ex.message;
            err.hidden = false;
        }
    });

    document.getElementById('btn-home').addEventListener('click', () => Views.showWorkspaces());
    document.getElementById('btn-logout').addEventListener('click', async () => {
        await Realtime.disconnect();
        Api.logout();
        location.reload();
    });

    document.getElementById('form-new-workspace').addEventListener('submit', async e => {
        e.preventDefault();
        const name = e.target.elements['name'].value.trim();
        const emailsRaw = e.target.elements['emails'].value.trim();
        const emails = emailsRaw
            ? emailsRaw.split(',').map(s => s.trim()).filter(Boolean)
            : null;
        const errEl = document.getElementById('ws-create-error');
        errEl.hidden = true;
        if (!name) return;
        try {
            await Api.createWorkspace(name, emails);
            e.target.reset();
            Views.showWorkspaces();
        } catch (ex) {
            errEl.textContent = ex.message;
            errEl.hidden = false;
        }
    });

    document.getElementById('form-invite-member').addEventListener('submit', async e => {
        e.preventDefault();
        const email = e.target.elements['email'].value.trim();
        const wsId = Views.getWorkspaceId();
        const errEl = document.getElementById('invite-error');
        errEl.hidden = true;
        if (!email || !wsId) return;
        try {
            await Api.inviteWorkspaceMember(wsId, email);
            e.target.reset();
            const wss = await Api.getWorkspaces();
            const ws = wss.find(w => w.id === wsId);
            if (ws) Views.showBoards(ws);
        } catch (ex) {
            errEl.textContent = ex.message;
            errEl.hidden = false;
        }
    });

    document.getElementById('form-new-board').addEventListener('submit', async e => {
        e.preventDefault();
        const name = e.target.elements['name'].value.trim();
        const wsId = Views.getWorkspaceId();
        if (!name || !wsId) return;
        try {
            await Api.createBoard(name, wsId);
            e.target.reset();
            const wss = await Api.getWorkspaces();
            const ws = wss.find(w => w.id === wsId);
            if (ws) Views.showBoards(ws);
        } catch (ex) { alert(ex.message); }
    });

    // Card modal
    document.getElementById('card-close').addEventListener('click', () => Board.closeModal());
    document.getElementById('card-save').addEventListener('click', () => Board.saveOpenCard());
    document.getElementById('card-delete').addEventListener('click', () => Board.deleteOpenCard());
    document.getElementById('card-comment-form').addEventListener('submit', e => {
        e.preventDefault();
        const f = e.target;
        const content = f.elements['content'].value.trim();
        if (!content) return;
        Board.postOpenComment(content);
        f.reset();
    });

    // Boot
    if (Api.isAuthenticated()) {
        Views.showWorkspaces();
    } else {
        Views.show('screen-auth');
    }
});

