document.addEventListener('DOMContentLoaded', function () {
    const vectorModal = document.getElementById('vectorModal');
    let currentValues = [];
    let currentModalData = null;

    document.querySelectorAll('.btn-show-vector').forEach(btn => {
        btn.addEventListener('click', function () {
            currentModalData = {
                seq: this.getAttribute('data-sequence'),
                len: this.getAttribute('data-length'),
                min: this.getAttribute('data-min'),
                max: this.getAttribute('data-max'),
                norm: this.getAttribute('data-norm'),
                raw: this.getAttribute('data-raw'),
                embedding: this.getAttribute('data-embedding')
            };
        });
    });

    if (vectorModal) {
        vectorModal.addEventListener('show.bs.modal', function (event) {
            try {
                // Use closest in case the click was on an inner element
                const related = event.relatedTarget?.closest('.btn-show-vector') || event.relatedTarget;
                let data = currentModalData;

                if (related && related.hasAttribute('data-sequence')) {
                    data = {
                        seq: related.getAttribute('data-sequence'),
                        len: related.getAttribute('data-length'),
                        min: related.getAttribute('data-min'),
                        max: related.getAttribute('data-max'),
                        norm: related.getAttribute('data-norm'),
                        raw: related.getAttribute('data-raw'),
                        embedding: related.getAttribute('data-embedding')
                    };
                }

                if (!data) return;

                document.getElementById('modalChunkSeq').textContent = data.seq ? (parseInt(data.seq) + 1) : '';
                document.getElementById('modalVecLen').textContent = data.len || '';
                document.getElementById('modalVecMin').textContent = data.min || '';
                document.getElementById('modalVecMax').textContent = data.max || '';
                document.getElementById('modalVecNorm').textContent = data.norm || '';

                if (data.raw && data.raw !== 'null') {
                    const rawArr = JSON.parse(data.raw);
                    document.getElementById('modalRawVals').textContent = (rawArr || []).join(", ");
                } else {
                    document.getElementById('modalRawVals').textContent = '';
                }

                if (data.embedding && data.embedding !== 'null') {
                    currentValues = JSON.parse(data.embedding) || [];
                } else {
                    currentValues = [];
                }

                const canvas = document.getElementById('modalHeatmap');
                const ctx = canvas.getContext('2d');
                ctx.clearRect(0, 0, canvas.width, canvas.height);
            } catch (e) {
                console.error('Error rendering vector details:', e);
            }
        });

        vectorModal.addEventListener('shown.bs.modal', function () {
            try {
                if (!currentValues || currentValues.length === 0) return;

                const canvas = document.getElementById('modalHeatmap');
                const displayWidth = canvas.getBoundingClientRect().width;
                if (displayWidth === 0) return;

                canvas.width = Math.floor(displayWidth);
                canvas.height = 40;

                const ctx = canvas.getContext('2d');
                const w = canvas.width / currentValues.length;

                const min = Math.min(...currentValues);
                const max = Math.max(...currentValues);
                const range = max - min || 1;

                currentValues.forEach((v, i) => {
                    const t = (v - min) / range;
                    // Tăng độ tương phản (đỏ/xanh đậm hơn, giảm xanh lá)
                    const r = Math.round(t * 255);
                    const b = Math.round((1 - t) * 255);
                    ctx.fillStyle = `rgb(${r}, 30, ${b})`;
                    
                    // Tính tọa độ nguyên để tránh anti-aliasing làm mờ viền (sub-pixel blurring)
                    const x1 = Math.floor(i * w);
                    const x2 = Math.floor((i + 1) * w);
                    ctx.fillRect(x1, 0, Math.max(x2 - x1, 1), canvas.height);
                });
            } catch (e) {
                console.error('Error drawing heatmap:', e);
            }
        });
    }

});
