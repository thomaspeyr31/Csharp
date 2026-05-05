// Board view: lists + cards + drag&drop + card detail modal.
const Board = (() => {
    let currentBoard = null;
    let lists = [];
    let cardsByList = {}; // listId -> array
    let openCardId = null;

    async function open(board) {
        currentBoard = board;
        Views.setBoardId(board.id);
        Views.setCrumb(document.querySelector('#crumb-ws strong')?.textContent, board.name);
        Views.show('screen-board');

        await Realtime.connectToBoard(board.id);
        await refresh();
    }

    async function refresh() {
        if (!currentBoard) return;
        lists = await Api.getLists(currentBoard.id);
        cardsByList = {};
        await Promise.all(lists.map(async l => {
            cardsByList[l.id] = await Api.getCards(l.id);
        }));
        render();
    }

    function render() {
        const root = document.getElementById('board-columns');
        root.innerHTML = '';
        lists.forEach(list => root.appendChild(renderList(list)));
        root.appendChild(renderAddListColumn());
    }

    function renderList(list) {
        const col = document.createElement('div');
        col.className = 'list-column';
        col.dataset.listId = list.id;

        col.innerHTML = `
            <div class="list-header">
                <span>${Views.escapeHtml(list.name)}</span>
                <span class="actions"><button title="Supprimer la liste">×</button></span>
            </div>
            <div class="cards"></div>
            <form class="add-card-form">
                <input name="title" placeholder="+ Ajouter une carte" required>
                <button type="submit">+</button>
            </form>
        `;

        col.querySelector('.actions button').addEventListener('click', async () => {
            if (!confirm(`Supprimer la liste "${list.name}" ?`)) return;
            try { await Api.deleteList(list.id); }
            catch (err) { alert(err.message); }
        });

        const cardsEl = col.querySelector('.cards');
        (cardsByList[list.id] || []).forEach(card => cardsEl.appendChild(renderCard(card)));

        col.addEventListener('dragover', e => { e.preventDefault(); col.classList.add('drag-over'); });
        col.addEventListener('dragleave', () => col.classList.remove('drag-over'));
        col.addEventListener('drop', async e => {
            e.preventDefault();
            col.classList.remove('drag-over');
            const cardId = parseInt(e.dataTransfer.getData('text/plain'), 10);
            if (!cardId) return;
            const card = findCard(cardId);
            if (!card || card.listId === list.id) return;
            try {
                await Api.updateCard(cardId, {
                    title: card.title,
                    description: card.description,
                    position: (cardsByList[list.id]?.length ?? 0),
                    dueDate: card.dueDate,
                    listId: list.id
                });
            } catch (err) {
                alert(err.message);
            }
        });

        col.querySelector('form').addEventListener('submit', async e => {
            e.preventDefault();
            const input = e.target.elements['title'];
            const title = input.value.trim();
            if (!title) return;
            try {
                await Api.createCard(title, (cardsByList[list.id]?.length ?? 0), list.id, null);
                input.value = '';
            } catch (err) {
                alert(err.message);
            }
        });

        return col;
    }

    function renderCard(card) {
        const el = document.createElement('div');
        el.className = 'card';
        el.draggable = true;
        el.dataset.cardId = card.id;
        const due = card.dueDate ? `<div class="due ${isOverdue(card.dueDate) ? 'overdue' : ''}">${formatDate(card.dueDate)}</div>` : '';
        el.innerHTML = `<div>${Views.escapeHtml(card.title)}</div>${due}`;
        el.addEventListener('dragstart', e => {
            e.dataTransfer.setData('text/plain', String(card.id));
            el.classList.add('dragging');
        });
        el.addEventListener('dragend', () => el.classList.remove('dragging'));
        el.addEventListener('click', () => openCardDetail(card.id));
        return el;
    }

    function renderAddListColumn() {
        const div = document.createElement('div');
        div.className = 'add-list-column';
        div.innerHTML = `
            <form class="add-list-form">
                <input name="name" placeholder="+ Ajouter une liste" required>
                <button type="submit">+</button>
            </form>
        `;
        div.querySelector('form').addEventListener('submit', async e => {
            e.preventDefault();
            const input = e.target.elements['name'];
            const name = input.value.trim();
            if (!name) return;
            try {
                await Api.createList(name, lists.length, currentBoard.id);
                input.value = '';
            } catch (err) {
                alert(err.message);
            }
        });
        return div;
    }

    function findCard(cardId) {
        for (const lid of Object.keys(cardsByList)) {
            const found = cardsByList[lid].find(c => c.id === cardId);
            if (found) return found;
        }
        return null;
    }

    function isOverdue(d) { return new Date(d) < new Date(); }
    function formatDate(d) {
        const dt = new Date(d);
        return dt.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short' });
    }

    async function openCardDetail(cardId) {
        try {
            const card = await Api.getCard(cardId);
            openCardId = cardId;
            document.getElementById('card-title').value = card.title;
            document.getElementById('card-desc').value = card.description ?? '';
            document.getElementById('card-due').value = card.dueDate
                ? new Date(card.dueDate).toISOString().slice(0, 16)
                : '';
            renderComments(card.comments ?? []);
            document.getElementById('card-modal').hidden = false;
        } catch (err) {
            alert(err.message);
        }
    }

    function renderComments(comments) {
        const ul = document.getElementById('card-comments');
        ul.innerHTML = '';
        comments.forEach(c => {
            const li = document.createElement('li');
            li.innerHTML = `<div class="meta">${Views.escapeHtml(c.authorUsername)} — ${new Date(c.createdAt).toLocaleString('fr-FR')}</div>
                            <div>${Views.escapeHtml(c.content)}</div>`;
            ul.appendChild(li);
        });
    }

    function closeModal() {
        document.getElementById('card-modal').hidden = true;
        openCardId = null;
    }

    async function saveOpenCard() {
        if (!openCardId) return;
        const card = findCard(openCardId);
        if (!card) return;
        const title = document.getElementById('card-title').value.trim() || card.title;
        const desc = document.getElementById('card-desc').value;
        const dueRaw = document.getElementById('card-due').value;
        const due = dueRaw ? new Date(dueRaw).toISOString() : null;
        try {
            await Api.updateCard(openCardId, {
                title, description: desc || null, position: card.position,
                dueDate: due, listId: card.listId
            });
            closeModal();
        } catch (err) {
            alert(err.message);
        }
    }

    async function deleteOpenCard() {
        if (!openCardId) return;
        if (!confirm('Supprimer cette carte ?')) return;
        try {
            await Api.deleteCard(openCardId);
            closeModal();
        } catch (err) {
            alert(err.message);
        }
    }

    async function postOpenComment(content) {
        if (!openCardId) return;
        try {
            await Api.createComment(openCardId, content);
            const card = await Api.getCard(openCardId);
            renderComments(card.comments ?? []);
        } catch (err) {
            alert(err.message);
        }
    }

    return {
        open, refresh, render, closeModal,
        saveOpenCard, deleteOpenCard, postOpenComment,
        getOpenCardId: () => openCardId,
        // Realtime hooks (called by realtime.js)
        onRemoteChange: () => refresh()
    };
})();

