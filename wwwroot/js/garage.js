window.initSortable = (dotnetRef) => {
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
