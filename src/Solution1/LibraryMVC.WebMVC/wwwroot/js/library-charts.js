
(function () {
    const palette = [
        "#1e3a5f",
        "#c4a35a",
        "#4f6d8c",
        "#8a6a32",
        "#6e8b74",
        "#a35d4a",
        "#7d6b8a",
        "#3d6b7a"
    ];

    if (window.Chart) {
        Chart.defaults.font.family = "Segoe UI, system-ui, sans-serif";
        Chart.defaults.color = "#2c2416";
    }

    function field(item, key) {
        if (!item) {
            return undefined;
        }

        if (item[key] != null) {
            return item[key];
        }

        const pascal = key.charAt(0).toUpperCase() + key.slice(1);
        if (item[pascal] != null) {
            return item[pascal];
        }

        const values = Object.values(item);
        return key === "count" ? values[1] : values[0];
    }

    async function loadSeries(url, labelKey, valueKey) {
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error("HTTP " + response.status);
        }

        const rows = await response.json();
        return {
            labels: rows.map(function (row) {
                return String(field(row, labelKey) ?? "");
            }),
            values: rows.map(function (row) {
                return Number(field(row, valueKey) ?? 0);
            })
        };
    }

    function showMessage(canvas, text) {
        const box = canvas.closest(".chart-box");
        const message = box ? box.querySelector(".chart-empty") : null;
        canvas.hidden = true;
        if (message) {
            message.textContent = text;
            message.classList.remove("d-none");
        }
    }

    function showCanvas(canvas) {
        const box = canvas.closest(".chart-box");
        const message = box ? box.querySelector(".chart-empty") : null;
        canvas.hidden = false;
        if (message) {
            message.classList.add("d-none");
        }
    }

    function mount(canvas, config) {
        const existing = typeof Chart.getChart === "function" ? Chart.getChart(canvas) : null;
        if (existing) {
            existing.destroy();
        }

        return new Chart(canvas, config);
    }

    function axisTitle(text) {
        return {
            display: Boolean(text),
            text: text,
            color: "#2c2416",
            font: { size: 12 }
        };
    }

    function valueTicks() {
        return { precision: 0, color: "#6c6458" };
    }

    function sliceColors(count) {
        const colors = [];
        for (let index = 0; index < count; index++) {
            colors.push(palette[index % palette.length]);
        }

        return colors;
    }

    async function draw(canvasId, url, labelKey, valueKey, build) {
        const canvas = document.getElementById(canvasId);
        if (!canvas || !window.Chart) {
            return;
        }

        try {
            const series = await loadSeries(url, labelKey, valueKey);
            if (series.labels.length === 0) {
                showMessage(canvas, "Немає даних");
                return;
            }

            showCanvas(canvas);
            mount(canvas, build(series));
        } catch (error) {
            showMessage(canvas, "Не вдалося завантажити дані");
        }
    }

    function categoryScales(mini, xTitle, yTitle, horizontal) {
        if (mini) {
            return {
                x: { display: false, beginAtZero: horizontal },
                y: { display: false, beginAtZero: !horizontal }
            };
        }

        if (horizontal) {
            return {
                x: { beginAtZero: true, title: axisTitle(yTitle), ticks: valueTicks() },
                y: { title: axisTitle(xTitle), ticks: { color: "#6c6458" } }
            };
        }

        return {
            x: { title: axisTitle(xTitle), ticks: { color: "#6c6458" } },
            y: { beginAtZero: true, title: axisTitle(yTitle), ticks: valueTicks() }
        };
    }

    window.LibraryCharts = {
        bar: function (canvasId, url, labelKey, valueKey, label, xTitle, yTitle, mini, horizontal) {
            return draw(canvasId, url, labelKey, valueKey, function (series) {
                return {
                    type: "bar",
                    data: {
                        labels: series.labels,
                        datasets: [{
                            label: label,
                            data: series.values,
                            backgroundColor: horizontal ? "#c4a35a" : "#1e3a5f",
                            borderRadius: 4,
                            maxBarThickness: mini ? 16 : 42
                        }]
                    },
                    options: {
                        indexAxis: horizontal ? "y" : "x",
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { display: false } },
                        scales: categoryScales(mini, xTitle, yTitle, horizontal)
                    }
                };
            });
        },

        line: function (canvasId, url, labelKey, valueKey, label, xTitle, yTitle, mini) {
            return draw(canvasId, url, labelKey, valueKey, function (series) {
                return {
                    type: "line",
                    data: {
                        labels: series.labels,
                        datasets: [{
                            label: label,
                            data: series.values,
                            borderColor: "#1e3a5f",
                            backgroundColor: "rgba(30, 58, 95, 0.15)",
                            fill: true,
                            tension: 0.25,
                            pointRadius: mini ? 2 : 3,
                            pointBackgroundColor: "#c4a35a"
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { display: false } },
                        scales: categoryScales(mini, xTitle, yTitle, false)
                    }
                };
            });
        },

        pie: function (canvasId, url, labelKey, valueKey, mini, doughnut) {
            return draw(canvasId, url, labelKey, valueKey, function (series) {
                return {
                    type: doughnut ? "doughnut" : "pie",
                    data: {
                        labels: series.labels,
                        datasets: [{
                            data: series.values,
                            backgroundColor: sliceColors(series.labels.length),
                            borderColor: "#fff",
                            borderWidth: 2
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        cutout: doughnut ? (mini ? "62%" : "55%") : 0,
                        plugins: {
                            legend: {
                                display: !mini,
                                position: "bottom",
                                labels: { boxWidth: 12, color: "#2c2416" }
                            }
                        }
                    }
                };
            });
        }
    };
})();
