document.addEventListener("DOMContentLoaded", function () {

    const btn = document.getElementById("themeToggle");

    if (localStorage.getItem("theme") === "dark") {
        document.body.classList.add("dark-mode");

        if (btn) {
            btn.innerHTML = "☀️";
        }
    }

    if (btn) {

        btn.addEventListener("click", function () {

            document.body.classList.toggle("dark-mode");

            if (document.body.classList.contains("dark-mode")) {

                localStorage.setItem("theme", "dark");
                btn.innerHTML = "☀️";

            }
            else {

                localStorage.setItem("theme", "light");
                btn.innerHTML = "🌙";

            }

        });

    }

});