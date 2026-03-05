// Global SweetAlert2 Confirmation Helper for Standard Forms
function confirmAction(event, message, confirmText = 'Confirm', cancelText = 'Cancel') {
    event.preventDefault();
    // Walk up the DOM to find the enclosing <form> element
    // (event.target may be an icon or span inside the button)
    var form = event.target.closest('form');
    if (!form) {
        console.error('confirmAction: no parent <form> found.');
        return false;
    }

    Swal.fire({
        title: 'Confirm Action',
        text: message,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: confirmText,
        cancelButtonText: cancelText
    }).then((result) => {
        if (result.isConfirmed) {
            form.submit();
        }
    });
    return false;
}

// Global SweetAlert2 Confirmation Helper for AJAX calls
function confirmAjaxAction(event, message, successCallback, confirmText = 'Confirm', cancelText = 'Cancel') {
    event.preventDefault();
    // Walk up the DOM to find the enclosing <form> element
    var form = event.target.closest('form');
    if (!form) {
        console.error('confirmAjaxAction: no parent <form> found.');
        return false;
    }

    Swal.fire({
        title: 'Confirm Action',
        text: message,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: confirmText,
        cancelButtonText: cancelText
    }).then((result) => {
        if (result.isConfirmed) {
            // Execute the AJAX fetch request
            fetch(form.action, {
                method: form.method,
                body: new FormData(form),
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                }
            })
                .then(response => {
                    if (!response.ok) throw new Error('Network error');
                    return response.json();
                })
                .then(data => {
                    if (data.success) {
                        Swal.fire('Success!', data.message || 'Operation completed successfully.', 'success');
                        if (successCallback) successCallback(form, data);
                    } else {
                        Swal.fire('Error!', data.message || 'An error occurred.', 'error');
                    }
                })
                .catch(error => {
                    console.error(error);
                    Swal.fire('Error!', 'Unable to connect to the server.', 'error');
                });
        }
    });
    return false;
}

// ==========================================
// SignalR Real-Time Notification Listener
// ==========================================
document.addEventListener("DOMContentLoaded", function () {
    // Check if SignalR is loaded (depends on _Layout including it)
    if (typeof signalR !== 'undefined') {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/hub/v1/notification")
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveNotification", function (title, message) {
            // Show a Toastr toast notification
            toastr.info(message, title, {
                timeOut: 6000,
                closeButton: true,
                progressBar: true,
                positionClass: "toast-top-right"
            });

            // Increment the notification badge counter
            let alertSpan = document.querySelector('.notif-badge');
            if (alertSpan) {
                let currentCount = parseInt(alertSpan.innerText);
                if (!isNaN(currentCount)) {
                    alertSpan.innerText = currentCount + 1;
                }
            } else {
                // Create the badge if it didn't exist before (0 notifications)
                let bellBtn = document.querySelector('.notif-bell-btn');
                if (bellBtn) {
                    let badge = document.createElement('span');
                    badge.className = 'notif-badge alert-count';
                    badge.innerText = '1';
                    bellBtn.prepend(badge);
                }
            }
        });

        connection.start().then(function () {
            console.log("SignalR Connected for Notifications.");
        }).catch(function (err) {
            return console.error("SignalR Connection Error: ", err.toString());
        });
    }
});
