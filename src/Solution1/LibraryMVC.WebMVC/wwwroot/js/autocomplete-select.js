$(function () {
    $(".js-lookup").each(function () {
        var $el = $(this);
        $el.select2({
            width: "100%",
            minimumInputLength: 3,
            ajax: {
                url: $el.data("lookupUrl"),
                dataType: "json",
                delay: 300,
                data: function (params) {
                    return { term: params.term };
                },
                processResults: function (data) {
                    return { results: data };
                }
            },
            language: {
                inputTooShort: function () {
                    return "Введіть щонайменше 3 символи";
                },
                searching: function () {
                    return "Пошук…";
                },
                noResults: function () {
                    return "Нічого не знайдено";
                },
                errorLoading: function () {
                    return "Не вдалося завантажити результати";
                }
            }
        });
    });
});
