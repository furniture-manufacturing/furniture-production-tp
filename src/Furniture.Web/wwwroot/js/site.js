const client = document.getElementById("ClientId");
const address = document.getElementById("ExecutionAddress");

if (client && address) {
  let lastDefaultAddress = client.selectedOptions[0]?.dataset.address ?? "";

  client.addEventListener("change", () => {
    const defaultAddress = client.selectedOptions[0]?.dataset.address ?? "";

    if (!address.value.trim() || address.value === lastDefaultAddress) {
      address.value = defaultAddress;
    }

    lastDefaultAddress = defaultAddress;
  });
}
