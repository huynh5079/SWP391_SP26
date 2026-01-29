// Global SweetAlert2 Confirmation Helper for Standard Forms
function confirmAction(event, message, confirmText = 'Đồng ý', cancelText = 'Hủy') {
    event.preventDefault(); // Prevent default form submission
    var form = event.target; // Get the form

    Swal.fire({
        title: 'Xác nhận',
        text: message,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: confirmText,
        cancelButtonText: cancelText
    }).then((result) => {
        if (result.isConfirmed) {
            form.submit(); // Submit the form programmatically
        }
    });
    return false; // Stop the initial submission
}

// Global SweetAlert2 Confirmation Helper for AJAX Forms
function confirmAjaxAction(event, message, successCallback, confirmText = 'Đồng ý', cancelText = 'Hủy') {
    console.log('confirmAjaxAction called');
    event.preventDefault();
    var form = event.target;

    Swal.fire({
        title: 'Xác nhận',
        text: message,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: confirmText,
        cancelButtonText: cancelText
    }).then((result) => {
        if (result.isConfirmed) {
            // Perform Fetch
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
                        Swal.fire('Thành công!', data.message || 'Thao tác thành công', 'success');
                        if (successCallback) successCallback(form, data);
                    } else {
                        Swal.fire('Lỗi!', data.message || 'Có lỗi xảy ra', 'error');
                    }
                })
                .catch(error => {
                    console.error(error);
                    Swal.fire('Lỗi!', 'Không thể kết nối tới máy chủ', 'error');
                });
        }
    });
    return false;
}
