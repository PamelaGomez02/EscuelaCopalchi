(function () {
    'use strict';

    // ---- Solo letras (tildes, ñ y espacios entre palabras) ----
    document.querySelectorAll('[data-solo-letras]').forEach(function (input) {
        input.addEventListener('input', function () {
            input.value = input.value
                .replace(/[^A-Za-zÁÉÍÓÚÜÑáéíóúüñ\s]/g, '')
                .replace(/^\s+/, '')
                .replace(/\s{2,}/g, ' ');
        });
    });

    // ---- Solo números con formato fijo ----
    function soloDigitos(valor, max) {
        return valor.replace(/\D/g, '').slice(0, max);
    }

    // Identificación: cédula (1-1111-1111) o DIMEX (11-12 dígitos, sin guiones)
    var idInput = document.querySelector('[data-formato="cedula"]');
    var chkExtranjero = document.getElementById('EsExtranjero');
    var ayudaId = document.getElementById('ayudaIdentificacion');

    function formatearCedula(valor) {
        var d = soloDigitos(valor, 9);
        return [d.slice(0, 1), d.slice(1, 5), d.slice(5, 9)]
            .filter(Boolean).join('-');
    }

    function aplicarModoIdentificacion() {
        var extranjero = chkExtranjero && chkExtranjero.checked;
        if (extranjero) {
            idInput.maxLength = 12;
            idInput.placeholder = '155812345678';
            idInput.pattern = '\\d{11,12}';
            idInput.title = 'El DIMEX debe tener 11 o 12 dígitos, sin guiones.';
            if (ayudaId) { ayudaId.textContent = 'Solo números, 11 o 12 dígitos, sin guiones.'; }
        } else {
            idInput.maxLength = 11;
            idInput.placeholder = '1-1111-1111';
            idInput.pattern = '\\d-\\d{4}-\\d{4}';
            idInput.title = 'La cédula debe tener el formato 1-1111-1111.';
            if (ayudaId) { ayudaId.textContent = 'Solo números. Formato: 1-1111-1111'; }
        }
    }

    if (idInput) {
        // Si el valor ya guardado es un DIMEX, la casilla arranca marcada
        if (chkExtranjero && /^\d{11,12}$/.test(idInput.value)) {
            chkExtranjero.checked = true;
        }
        aplicarModoIdentificacion();

        idInput.addEventListener('input', function () {
            idInput.value = (chkExtranjero && chkExtranjero.checked)
                ? soloDigitos(idInput.value, 12)
                : formatearCedula(idInput.value);
        });

        if (chkExtranjero) {
            chkExtranjero.addEventListener('change', function () {
                idInput.value = '';          // al cambiar de tipo se vuelve a escribir
                aplicarModoIdentificacion();
                idInput.focus();
            });
        }
    }

    // Teléfono: 8888-8888
    document.querySelectorAll('[data-formato="telefono"]').forEach(function (input) {
        input.addEventListener('input', function () {
            var d = soloDigitos(input.value, 8);
            input.value = [d.slice(0, 4), d.slice(4, 8)]
                .filter(Boolean).join('-');
        });
    });

    // ---- Quitar espacios sobrantes al enviar ----
    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function () {
            form.querySelectorAll('[data-solo-letras]').forEach(function (i) {
                i.value = i.value.trim();
            });
        });
    });

    // ---- Ojito para mostrar / ocultar contraseña ----
    document.querySelectorAll('[data-toggle-password]').forEach(function (boton) {
        boton.addEventListener('click', function () {
            var input = document.getElementById(boton.getAttribute('data-toggle-password'));
            var icono = boton.querySelector('i');
            var mostrar = input.type === 'password';
            input.type = mostrar ? 'text' : 'password';
            icono.className = mostrar ? 'bi bi-eye-slash' : 'bi bi-eye';
        });
    });

    // ---- Confirmación de contraseña ----
    var pass = document.getElementById('Contrasena');
    var confirmar = document.getElementById('ConfirmarContrasena');
    var aviso = document.getElementById('avisoConfirmacion');

    if (pass && confirmar) {
        var validarConfirmacion = function () {
            var coinciden = pass.value === confirmar.value;
            var mostrarAviso = confirmar.value !== '' && !coinciden;
            confirmar.classList.toggle('is-invalid', mostrarAviso);
            if (aviso) { aviso.style.display = mostrarAviso ? 'block' : 'none'; }
            return coinciden;
        };

        pass.addEventListener('input', validarConfirmacion);
        confirmar.addEventListener('input', validarConfirmacion);

        confirmar.form.addEventListener('submit', function (e) {
            if (!validarConfirmacion()) {
                e.preventDefault();
                confirmar.focus();
            }
        });
    }
})();
