const { createApp, ref } = Vue;

createApp({
  setup() {
    const activeNames = ref(['quick', 'mapping', 'fk']);
    return { activeNames };
  }
}).use(ElementPlus).mount('#app');

