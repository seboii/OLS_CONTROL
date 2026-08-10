<template>
  <div class="spinner"></div>
</template>

<style scoped>
.spinner {
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: radial-gradient(farthest-side, var(--color-primary) 94%, #0000) top/3px 3px no-repeat, conic-gradient(#0000 30%, var(--color-primary));
  -webkit-mask: radial-gradient(farthest-side, #0000 calc(100% - 3px), #000 0);
  animation: spinner-c7wet2 1s infinite linear;
}

@keyframes spinner-c7wet2 {
  100% {
    transform: rotate(1turn);
  }
}
</style>
