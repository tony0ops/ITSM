document.addEventListener('DOMContentLoaded', function () {
    const group = document.getElementById('Ticket_AssignedGroup');
    const assignee = document.getElementById('Ticket_AssigneeUserId');
    if (!group || !assignee) return;
    let requestVersion = 0;
    const message = document.createElement('div');
    message.className = 'form-text';
    message.setAttribute('role', 'status');
    assignee.after(message);
    async function reload() {
        const version = ++requestVersion;
        const previous = assignee.value;
        assignee.replaceChildren(new Option('-- Unassigned --', ''));
        assignee.disabled = true;
        message.textContent = 'Loading assignees...';
        const url = new URL(window.location.href);
        url.searchParams.set('handler', 'Assignees');
        url.searchParams.set('group', group.value);
        try {
            const response = await fetch(url, { headers: { 'Accept': 'application/json' } });
            if (!response.ok) throw new Error('Could not load assignees');
            const choices = await response.json();
            if (version !== requestVersion) return;
            for (const choice of choices) assignee.add(new Option(choice.text, choice.value));
            if (choices.some(c => c.value === previous)) assignee.value = previous;
            message.textContent = choices.length ? '' : 'No active staff available for this group.';
        } catch (_) {
            if (version === requestVersion) message.textContent = 'Could not load assignees. Reload the page to try again.';
        } finally {
            if (version === requestVersion) assignee.disabled = false;
        }
    }
    group.addEventListener('change', reload);
    if (group.value === '0') reload();
});
