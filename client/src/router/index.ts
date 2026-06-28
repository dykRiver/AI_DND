import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: () => import('@/views/Login.vue'),
    },
    {
      path: '/',
      name: 'Lobby',
      component: () => import('@/views/Lobby.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/dungeon-select',
      name: 'DungeonSelect',
      component: () => import('@/views/DungeonSelect.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/character-create',
      name: 'CharacterCreate',
      component: () => import('@/views/CharacterCreate.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/game',
      name: 'GameMain',
      component: () => import('@/views/GameMain.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/character',
      name: 'CharacterPanel',
      component: () => import('@/views/CharacterPanel.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/settlement/:sessionId',
      name: 'Settlement',
      component: () => import('@/views/Settlement.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/meta',
      name: 'MetaGrowth',
      component: () => import('@/views/MetaGrowth.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/rank',
      name: 'RankPage',
      component: () => import('@/views/RankPage.vue'),
      meta: { requiresAuth: true },
    },
  ],
})

// 路由守卫
router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore()
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next('/login')
  } else if (to.path === '/login' && authStore.isAuthenticated) {
    next('/')
  } else {
    next()
  }
})

export default router
