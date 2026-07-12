import { createRouter, createWebHistory } from 'vue-router'
import ContactCreatePage from './features/contacts/pages/ContactCreatePage.vue'
import ContactEditPage from './features/contacts/pages/ContactEditPage.vue'
import ContactListPage from './features/contacts/pages/ContactListPage.vue'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      redirect: { name: 'contacts-list' },
    },
    {
      path: '/contacts',
      name: 'contacts-list',
      component: ContactListPage,
    },
    {
      path: '/contacts/new',
      name: 'contacts-create',
      component: ContactCreatePage,
    },
    {
      path: '/contacts/:id/edit',
      name: 'contacts-edit',
      component: ContactEditPage,
      props: true,
    },
  ],
})
