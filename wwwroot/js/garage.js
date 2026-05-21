window.initSortable = (dotnetRef) => {
    if (typeof Sortable === 'undefined') return;
    const el = document.getElementById('garagen-sortable');
    if (!el) return;
    if (el._sortable) {
        el._sortable.destroy();
    }
    el._sortable = Sortable.create(el, {
        animation: 150,
        handle: '.drag-handle',
        ghostClass: 'drag-ghost',
        onEnd: function (evt) {
            dotnetRef.invokeMethodAsync('OnReorder', evt.oldIndex, evt.newIndex);
        }
    });
};

window.initFahrzeugSortable = (dotnetRef, garageIndex) => {
    if (typeof Sortable === 'undefined') return;
    const el = document.getElementById('fahrzeug-sortable-' + garageIndex);
    if (!el) return;
    if (el._sortable) {
        el._sortable.destroy();
    }
    el._sortable = Sortable.create(el, {
        animation: 150,
        handle: '.fahrzeug-drag-handle',
        ghostClass: 'drag-ghost',
        onEnd: function (evt) {
            dotnetRef.invokeMethodAsync('OnFahrzeugReorder', garageIndex, evt.oldIndex, evt.newIndex);
        }
    });
};