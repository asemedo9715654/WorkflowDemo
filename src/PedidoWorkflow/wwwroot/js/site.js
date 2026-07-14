// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.querySelectorAll(".app-alert").forEach((alert) => {
  window.setTimeout(() => {
    alert.classList.add("fade");
    alert.classList.remove("show");
  }, 4500);
});
